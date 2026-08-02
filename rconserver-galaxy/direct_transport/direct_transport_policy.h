#pragma once

#include <bf2direct/protocol.h>

namespace bf2direct
{

struct PolicySelection
{
	Policy value{Policy::Disabled};
	bool configured{};
	bool valid{};
};

PolicySelection ReadPolicy() noexcept;
const char *PolicyName(Policy policy) noexcept;

} // namespace bf2direct
