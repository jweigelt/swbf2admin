#pragma once

#include <Windows.h>

#include <cstddef>
#include <cstdint>
#include <initializer_list>
#include <span>
#include <vector>

class PatchEngine final
{
public:
	explicit PatchEngine(HMODULE module);

	[[nodiscard]] std::byte *at(std::uintptr_t rva) const;

	bool bytes(std::uintptr_t rva, std::span<const std::uint8_t> replacement) const;

	bool bytes(std::uintptr_t rva, std::initializer_list<std::uint8_t> replacement) const;

	// Redirects a five-byte relative call through a nearby relay.
	bool call(std::uintptr_t rva, void *replacement) const;

	// Installs a hook and returns a trampoline. Stolen instructions must be position independent; see PORTING.md.
	bool detour(std::uintptr_t rva, std::size_t stolenLength, void *replacement, void **original) const;

	// Replaces a complete entry path. No trampoline is produced.
	bool replace(std::uintptr_t rva, std::size_t overwriteLength, void *replacement) const;

private:
	bool write(std::uintptr_t rva, std::span<const std::uint8_t> data) const;
	void *allocateNear(const void *target, std::size_t size) const;

	std::uintptr_t base_{};
};
