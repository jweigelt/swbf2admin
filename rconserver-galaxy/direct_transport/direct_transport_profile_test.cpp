#include "direct_transport/direct_transport_profile.h"

#include <Windows.h>

#include <cstdio>
#include <filesystem>
#include <string>

int wmain(int argc, wchar_t **argv)
{
	if (argc != 2) return 2;
	const auto module =
		LoadLibraryExW(std::filesystem::absolute(argv[1]).c_str(), nullptr, DONT_RESOLVE_DLL_REFERENCES);
	if (!module) return 3;
	std::string error;
	const auto &profile = bf2direct::GetServerProfile();
	const auto valid = bf2direct::VerifyServerProfile(module, profile, error);
	if (!valid) std::fprintf(stderr, "%s\n", error.c_str());
	FreeLibrary(module);
	return valid ? 0 : 1;
}
