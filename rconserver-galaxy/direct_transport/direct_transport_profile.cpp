#include "direct_transport/direct_transport_profile.h"

#include <bf2direct/image.h>

#include <array>
#include <initializer_list>

namespace bf2direct
{
namespace
{

constexpr std::array<HookSite, 6> kHooks{{
	{HookId::FinalSend,
	 "final online send",
	 0x00218EC0,
	 {0x55, 0x8B, 0xEC, 0x83, 0xE4, 0xF8, 0x83, 0xEC, 0x0C, 0x53, 0x56, 0x57, 0x8B, 0xFA, 0x8B, 0xF1},
	 16},
	{HookId::GroupSend,
	 "native group send",
	 0x001B38A0,
	 {0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x08, 0x53, 0x56, 0x57, 0x8B, 0xF9, 0x89, 0x55, 0xF8, 0x33, 0xF6},
	 16},
	{HookId::ReceiveOrchestration, "native receive orchestration", 0x001B4170, {0x55, 0x8B, 0xEC, 0x51, 0x53, 0x56}, 6},
	{HookId::NativeIntake,
	 "native intake",
	 0x001B4240,
	 {0x55, 0x8B, 0xEC, 0x51, 0x53, 0x56, 0x57, 0x8B, 0xF9, 0x8B, 0xDA, 0x89, 0x5D, 0xFC, 0x0F, 0xBE},
	 16},
	{HookId::Disconnect,
	 "native disconnect",
	 0x001B3A90,
	 {0x55, 0x8B, 0xEC, 0x83, 0xE4, 0xF8, 0x51, 0x53, 0x8B, 0xD9, 0xB9, 0x40, 0xCE, 0xEC, 0x01, 0x56},
	 11},
	{HookId::Reset,
	 "native reset",
	 0x001B33D0,
	 {0x55, 0x8B, 0xEC, 0x83, 0xE4, 0xF8, 0x83, 0xEC, 0x08, 0x53, 0x56, 0x8A, 0xD9},
	 13},
}};

constexpr ServerProfile kProfile{
	"gog-galaxy-59edf52b-x86",
	0x59EDF52B,
	0x01BE5000,
	0x00400000,
	kHooks,
	0x001D5D70,
	0x003AC8FC,
	0x001D5CE0,
	0x003AC7D0,
	0x003E9EF4,
	0x00218C20,
	{0xFF, 0x15, 0x90, 0xC0, 0x76, 0x00, 0x85, 0xC0, 0x75, 0x01, 0xC3,
	 0xFF, 0x15, 0x90, 0xC0, 0x76, 0x00, 0x8B, 0xC8, 0x8B, 0x10},
	0x01ACD590,
	0x001D9EA0,
	{0x53, 0x84, 0xC9, 0xB8, 0x0C, 0x92, 0xEE, 0x01, 0xBB, 0x28, 0x92, 0xEE, 0x01, 0x0F, 0x44, 0xD8},
	0x001DA010,
	{0x8B, 0xC1, 0xC7, 0x41, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x2D, 0x10, 0x66, 0xEA, 0x01, 0xC7, 0x41},
	0x01BACD80,
	0x001C4530,
	{0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x18, 0xE8, 0xA5, 0x03, 0xE5, 0xFF, 0x0F, 0xB6, 0xC0, 0x85, 0xC0},
	0x001D1D90,
	{0x55, 0x8B, 0xEC, 0x51, 0x6A, 0x00, 0x8B, 0x45, 0x08, 0x8B, 0x08, 0x51, 0xE8, 0x8F, 0x27, 0xFF},
	0x001B9440,
	{0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x08, 0xB8, 0x01, 0x00, 0x00, 0x00, 0x33, 0xD2, 0x8B, 0x4D, 0x08},
};

bool VerifyBytes(HMODULE module, std::uint32_t rva, const std::uint8_t *bytes, std::size_t length, const char *name,
				 std::string &error) noexcept
{
	if (!VerifyImageBytes(module, rva, {bytes, length}))
	{
		error = std::string(name) + " bytes differ";
		return false;
	}
	return true;
}

bool VerifyGetNetworking(HMODULE module, const ServerProfile &profile, std::string &error) noexcept
{
	std::array relocated = profile.getNetworkingBytes;
	PeIdentity identity{};
	if (!ReadPeIdentity(module, identity))
	{
		error = "Galaxy GetNetworking image is unavailable";
		return false;
	}
	constexpr std::array offsets{std::size_t{2}, std::size_t{13}};
	if (!RelocateExpectedAddresses(relocated, offsets, reinterpret_cast<std::uintptr_t>(module),
								   profile.preferredImageBase))
	{
		error = "Galaxy GetNetworking bytes differ";
		return false;
	}
	return VerifyBytes(module, profile.getNetworkingRva, relocated.data(), relocated.size(), "Galaxy GetNetworking",
					   error);
}

bool VerifyPacketFunction(HMODULE module, const ServerProfile &profile, std::uint32_t rva,
						  const std::array<std::uint8_t, 16> &bytes,
						  std::initializer_list<std::size_t> relocationOffsets, const char *name,
						  std::string &error) noexcept
{
	std::array relocated = bytes;
	if (!RelocateExpectedAddresses(relocated, {relocationOffsets.begin(), relocationOffsets.size()},
								   reinterpret_cast<std::uintptr_t>(module), profile.preferredImageBase))
	{
		error = std::string(name) + " bytes differ";
		return false;
	}
	return VerifyBytes(module, rva, relocated.data(), relocated.size(), name, error);
}

} // namespace

const ServerProfile &GetServerProfile() noexcept
{
	return kProfile;
}

bool VerifyServerProfile(HMODULE module, const ServerProfile &profile, std::string &error) noexcept
{
	PeIdentity identity{};
	if (!ReadPeIdentity(module, identity) || identity.machine != IMAGE_FILE_MACHINE_I386 ||
		identity.timestamp != profile.timestamp || identity.imageSize != profile.imageSize)
	{
		error = "GOG server executable identity differs";
		return false;
	}
	for (const auto &hook : profile.hooks)
	{
		if (!VerifyBytes(module, hook.rva, hook.bytes.data(), hook.length, hook.name, error))
		{
			return false;
		}
	}
	constexpr std::array<std::uint8_t, 3> remoteReturn{0xC2, 0x0C, 0x00};
	if (!VerifyBytes(module, profile.remoteMemberCallbackRva, remoteReturn.data(), remoteReturn.size(),
					 "remote member callback", error))
	{
		return false;
	}
	constexpr std::array<std::uint8_t, 6> localEntry{0x55, 0x8B, 0xEC, 0x83, 0xE4, 0xF8};
	if (!VerifyBytes(module, profile.localLobbyLeftCallbackRva, localEntry.data(), localEntry.size(),
					 "local lobby-left callback", error))
	{
		return false;
	}
	if (!VerifyGetNetworking(module, profile, error) ||
		!VerifyPacketFunction(module, profile, profile.packetAllocateRva, profile.packetAllocateBytes, {4, 9},
							  "stock packet allocator", error) ||
		!VerifyPacketFunction(module, profile, profile.packetInitializeRva, profile.packetInitializeBytes, {10},
							  "stock packet initializer", error) ||
		!VerifyBytes(module, profile.isPlayingRva, profile.isPlayingBytes.data(), profile.isPlayingBytes.size(),
					 "IsPlaying", error) ||
		!VerifyBytes(module, profile.updateBootPlayerRva, profile.updateBootPlayerBytes.data(),
					 profile.updateBootPlayerBytes.size(), "UpdateBootPlayer", error) ||
		!VerifyBytes(module, profile.setNotPlayingRva, profile.setNotPlayingBytes.data(),
					 profile.setNotPlayingBytes.size(), "SetNotPlaying", error))
	{
		return false;
	}
	auto *const base = reinterpret_cast<std::byte *>(module);
	if (!ReadableMemoryRange(base + profile.gamePortRva, sizeof(std::uint32_t)))
	{
		error = "/gameport storage is unavailable";
		return false;
	}
	constexpr std::size_t endpointRecordBytes = 67 * 0x60;
	if (profile.endpointTableRva < 0x20 ||
		!ReadableMemoryRange(base + profile.endpointTableRva - 0x20, endpointRecordBytes) ||
		!ReadableMemoryRange(base + profile.packetPrefixBytesRva, sizeof(std::uint32_t)))
	{
		error = "native endpoint records are unavailable";
		return false;
	}
	auto *const remoteSlot = reinterpret_cast<void **>(base + profile.remoteMemberListenerSlotRva);
	auto *const localSlot = reinterpret_cast<void **>(base + profile.localLobbyLeftListenerSlotRva);
	if (!ReadableMemoryRange(remoteSlot, sizeof(*remoteSlot)) || *remoteSlot != base + profile.remoteMemberCallbackRva)
	{
		error = "remote member listener slot differs";
		return false;
	}
	if (!ReadableMemoryRange(localSlot, sizeof(*localSlot)) || *localSlot != base + profile.localLobbyLeftCallbackRva)
	{
		error = "local lobby-left listener slot differs";
		return false;
	}
	return true;
}

} // namespace bf2direct
