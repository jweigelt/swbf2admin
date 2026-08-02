#include "direct_transport/direct_transport_policy.h"

#include <Windows.h>

#include <array>
#include <cstring>

namespace bf2direct
{

PolicySelection ReadPolicy() noexcept
{
	std::array<char, 64> value{};
	const auto length = GetEnvironmentVariableA("BF2_DIRECT_POLICY", value.data(), static_cast<DWORD>(value.size()));
	if (!length) return {};
	if (length >= value.size()) return {Policy::Disabled, true, false};

	PolicySelection selection{};
	selection.configured = true;
	selection.valid = true;
	if (_stricmp(value.data(), "Disabled") == 0)
	{
		selection.value = Policy::Disabled;
	}
	else if (_stricmp(value.data(), "PreferDirect") == 0)
	{
		selection.value = Policy::PreferDirect;
	}
	else if (_stricmp(value.data(), "RequireDirectPatched") == 0)
	{
		selection.value = Policy::RequireDirectPatched;
	}
	else if (_stricmp(value.data(), "RequireDirectAll") == 0)
	{
		selection.value = Policy::RequireDirectAll;
	}
	else
	{
		selection.value = Policy::Disabled;
		selection.valid = false;
	}
	return selection;
}

const char *PolicyName(Policy policy) noexcept
{
	switch (policy)
	{
	case Policy::Disabled:
		return "Disabled";
	case Policy::PreferDirect:
		return "PreferDirect";
	case Policy::RequireDirectPatched:
		return "RequireDirectPatched";
	case Policy::RequireDirectAll:
		return "RequireDirectAll";
	default:
		return "Disabled";
	}
}

} // namespace bf2direct
