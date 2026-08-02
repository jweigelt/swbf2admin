#include "direct_transport/direct_transport_policy.h"

#include <Windows.h>

#include <array>
#include <cstring>

namespace
{

bool Check(const char *value, bf2direct::Policy expected, bool configured, bool valid) noexcept
{
	SetEnvironmentVariableA("BF2_DIRECT_POLICY", value);
	const auto selected = bf2direct::ReadPolicy();
	return selected.value == expected && selected.configured == configured && selected.valid == valid;
}

} // namespace

int main()
{
	if (!Check(nullptr, bf2direct::Policy::Disabled, false, false)) return 1;
	if (!Check("Disabled", bf2direct::Policy::Disabled, true, true)) return 2;
	if (!Check("PreferDirect", bf2direct::Policy::PreferDirect, true, true)) return 3;
	if (!Check("RequireDirectPatched", bf2direct::Policy::RequireDirectPatched, true, true)) return 4;
	if (!Check("RequireDirectAll", bf2direct::Policy::RequireDirectAll, true, true)) return 5;
	if (!Check("preferdirect", bf2direct::Policy::PreferDirect, true, true)) return 6;
	if (!Check("invalid", bf2direct::Policy::Disabled, true, false)) return 7;
	std::array<char, 80> oversized{};
	std::memset(oversized.data(), 'x', oversized.size() - 1);
	if (!Check(oversized.data(), bf2direct::Policy::Disabled, true, false)) return 8;
	return 0;
}
