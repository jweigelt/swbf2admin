#include "PatchEngine.h"

#include "Logger.h"

#include <algorithm>
#include <array>
#include <cstring>
#include <limits>

namespace
{
constexpr std::size_t kAbsoluteJumpSize = 12;

// MOV RAX,<destination>; JMP RAX. This works anywhere in the x64 address space.
std::array<std::uint8_t, kAbsoluteJumpSize> absolute_jump(const void *destination)
{
	std::array<std::uint8_t, kAbsoluteJumpSize> code{0x48, 0xB8, 0, 0, 0, 0, 0, 0, 0, 0, 0xFF, 0xE0};
	const auto value = reinterpret_cast<std::uint64_t>(destination);
	std::memcpy(code.data() + 2, &value, sizeof(value));
	return code;
}
} // namespace

PatchEngine::PatchEngine(HMODULE module) : base_(reinterpret_cast<std::uintptr_t>(module)) {}

std::byte *PatchEngine::at(std::uintptr_t rva) const
{
	return reinterpret_cast<std::byte *>(base_ + rva);
}

bool PatchEngine::write(std::uintptr_t rva, std::span<const std::uint8_t> data) const
{
	if (data.empty())
	{
		Logger.log(LogLevel_ERROR, "Invalid empty patch at Battlefront2.dll+0x%llX",
				   static_cast<unsigned long long>(rva));
		return false;
	}

	DWORD oldProtection{};
	void *address = at(rva);
	if (!VirtualProtect(address, data.size(), PAGE_EXECUTE_READWRITE, &oldProtection))
	{
		Logger.log(LogLevel_ERROR, "Patch write failed at Battlefront2.dll+0x%llX",
				   static_cast<unsigned long long>(rva));
		return false;
	}
	std::memcpy(address, data.data(), data.size());
	FlushInstructionCache(GetCurrentProcess(), address, data.size());
	DWORD ignored{};
	VirtualProtect(address, data.size(), oldProtection, &ignored);
	return true;
}

bool PatchEngine::bytes(std::uintptr_t rva, std::span<const std::uint8_t> replacement) const
{
	return write(rva, replacement);
}

bool PatchEngine::bytes(std::uintptr_t rva, std::initializer_list<std::uint8_t> replacement) const
{
	return bytes(rva, std::span(replacement.begin(), replacement.size()));
}

void *PatchEngine::allocateNear(const void *target, std::size_t size) const
{
	// A five-byte E9 relay must remain within signed rel32 range of the target.
	SYSTEM_INFO info{};
	GetSystemInfo(&info);
	const auto granularity = static_cast<std::uintptr_t>(info.dwAllocationGranularity);
	const auto origin = reinterpret_cast<std::uintptr_t>(target);
	const auto aligned = origin & ~(granularity - 1);
	constexpr std::uintptr_t kRange = 0x7FFF0000ULL;

	for (std::uintptr_t delta = granularity; delta < kRange; delta += granularity)
	{
		if (aligned >= delta)
		{
			if (void *result = VirtualAlloc(reinterpret_cast<void *>(aligned - delta), size, MEM_RESERVE | MEM_COMMIT,
											PAGE_EXECUTE_READWRITE))
			{
				return result;
			}
		}
		if (aligned <= std::numeric_limits<std::uintptr_t>::max() - delta)
		{
			if (void *result = VirtualAlloc(reinterpret_cast<void *>(aligned + delta), size, MEM_RESERVE | MEM_COMMIT,
											PAGE_EXECUTE_READWRITE))
			{
				return result;
			}
		}
	}
	return nullptr;
}

bool PatchEngine::call(std::uintptr_t rva, void *replacement) const
{
	auto *relay = static_cast<std::uint8_t *>(allocateNear(at(rva), 32));
	if (!relay)
	{
		Logger.log(LogLevel_ERROR, "Unable to allocate near relay at Battlefront2.dll+0x%llX",
				   static_cast<unsigned long long>(rva));
		return false;
	}

	const auto relayJump = absolute_jump(replacement);
	std::memcpy(relay, relayJump.data(), relayJump.size());
	FlushInstructionCache(GetCurrentProcess(), relay, relayJump.size());
	const auto displacement =
		static_cast<std::int64_t>(reinterpret_cast<std::uintptr_t>(relay)) - static_cast<std::int64_t>(base_ + rva + 5);
	if (displacement < INT32_MIN || displacement > INT32_MAX)
	{
		VirtualFree(relay, 0, MEM_RELEASE);
		Logger.log(LogLevel_ERROR, "Near relay is out of range at Battlefront2.dll+0x%llX",
				   static_cast<unsigned long long>(rva));
		return false;
	}

	std::array<std::uint8_t, 5> patch{0xE8, 0, 0, 0, 0};
	const auto rel32 = static_cast<std::int32_t>(displacement);
	std::memcpy(patch.data() + 1, &rel32, sizeof(rel32));
	if (!write(rva, patch))
	{
		VirtualFree(relay, 0, MEM_RELEASE);
		return false;
	}
	return true;
}

bool PatchEngine::detour(std::uintptr_t rva, std::size_t stolenLength, void *replacement, void **original) const
{
	if (!original || stolenLength < 5)
	{
		Logger.log(LogLevel_ERROR, "Invalid detour at Battlefront2.dll+0x%llX", static_cast<unsigned long long>(rva));
		return false;
	}

	// Copy the position-independent prologue and return after the stolen instructions.
	auto *trampoline = static_cast<std::uint8_t *>(
		VirtualAlloc(nullptr, stolenLength + kAbsoluteJumpSize, MEM_RESERVE | MEM_COMMIT, PAGE_EXECUTE_READWRITE));
	if (!trampoline)
	{
		Logger.log(LogLevel_ERROR, "Unable to allocate trampoline at Battlefront2.dll+0x%llX",
				   static_cast<unsigned long long>(rva));
		return false;
	}
	std::memcpy(trampoline, at(rva), stolenLength);
	const auto returnJump = absolute_jump(at(rva + stolenLength));
	std::memcpy(trampoline + stolenLength, returnJump.data(), returnJump.size());

	std::vector<std::uint8_t> patch(stolenLength, 0x90);
	if (stolenLength >= kAbsoluteJumpSize)
	{
		const auto jump = absolute_jump(replacement);
		std::copy(jump.begin(), jump.end(), patch.begin());
	}
	else
	{
		// Short prologues use a nearby relay for the unrestricted absolute jump.
		auto *relay = static_cast<std::uint8_t *>(allocateNear(at(rva), 32));
		if (!relay)
		{
			VirtualFree(trampoline, 0, MEM_RELEASE);
			Logger.log(LogLevel_ERROR, "Unable to allocate near relay at Battlefront2.dll+0x%llX",
					   static_cast<unsigned long long>(rva));
			return false;
		}
		const auto relayJump = absolute_jump(replacement);
		std::memcpy(relay, relayJump.data(), relayJump.size());
		const auto displacement = static_cast<std::int64_t>(reinterpret_cast<std::uintptr_t>(relay)) -
								  static_cast<std::int64_t>(base_ + rva + 5);
		if (displacement < INT32_MIN || displacement > INT32_MAX)
		{
			VirtualFree(relay, 0, MEM_RELEASE);
			VirtualFree(trampoline, 0, MEM_RELEASE);
			Logger.log(LogLevel_ERROR, "Near relay is out of range at Battlefront2.dll+0x%llX",
					   static_cast<unsigned long long>(rva));
			return false;
		}
		patch[0] = 0xE9;
		const auto rel32 = static_cast<std::int32_t>(displacement);
		std::memcpy(patch.data() + 1, &rel32, sizeof(rel32));
	}

	if (!write(rva, patch))
	{
		VirtualFree(trampoline, 0, MEM_RELEASE);
		return false;
	}
	*original = trampoline;
	return true;
}

bool PatchEngine::replace(std::uintptr_t rva, std::size_t overwriteLength, void *replacement) const
{
	if (overwriteLength < kAbsoluteJumpSize)
	{
		Logger.log(LogLevel_ERROR, "Invalid replacement at Battlefront2.dll+0x%llX",
				   static_cast<unsigned long long>(rva));
		return false;
	}
	std::vector<std::uint8_t> patch(overwriteLength, 0x90);
	const auto jump = absolute_jump(replacement);
	std::copy(jump.begin(), jump.end(), patch.begin());
	if (!write(rva, patch))
	{
		return false;
	}
	return true;
}
