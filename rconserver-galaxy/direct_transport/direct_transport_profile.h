#pragma once

#include <bf2direct/support.h>

#include <array>
#include <cstddef>
#include <cstdint>
#include <span>
#include <string>

#include <Windows.h>

namespace bf2direct
{

struct ServerProfile
{
	const char *id{};
	std::uint32_t timestamp{};
	std::uint32_t imageSize{};
	std::uint32_t preferredImageBase{};
	std::span<const HookSite> hooks;
	std::uint32_t remoteMemberCallbackRva{};
	std::uint32_t remoteMemberListenerSlotRva{};
	std::uint32_t localLobbyLeftCallbackRva{};
	std::uint32_t localLobbyLeftListenerSlotRva{};
	std::uint32_t gamePortRva{};
	std::uint32_t getNetworkingRva{};
	std::array<std::uint8_t, 21> getNetworkingBytes{};
	std::uint32_t endpointTableRva{};
	std::uint32_t packetAllocateRva{};
	std::array<std::uint8_t, 16> packetAllocateBytes{};
	std::uint32_t packetInitializeRva{};
	std::array<std::uint8_t, 16> packetInitializeBytes{};
	std::uint32_t packetPrefixBytesRva{};
	std::uint32_t isPlayingRva{};
	std::array<std::uint8_t, 16> isPlayingBytes{};
	std::uint32_t updateBootPlayerRva{};
	std::array<std::uint8_t, 16> updateBootPlayerBytes{};
	std::uint32_t setNotPlayingRva{};
	std::array<std::uint8_t, 16> setNotPlayingBytes{};
};

const ServerProfile &GetServerProfile() noexcept;
bool VerifyServerProfile(HMODULE module, const ServerProfile &profile, std::string &error) noexcept;
} // namespace bf2direct
