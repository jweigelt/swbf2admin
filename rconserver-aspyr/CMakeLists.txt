cmake_minimum_required(VERSION 3.10)
project(RconServer_64)

set(CMAKE_BUILD_TYPE Release)
set(CMAKE_GENERATOR_PLATFORM amd64)
set(CMAKE_SHARED_LIBRARY_PREFIX "")

set(SOURCES
        dllmain.cpp
        bf2server.cpp
        Logger.cpp
        )

add_library(RconServer_64 SHARED ${SOURCES})
target_compile_definitions(RconServer_64 PRIVATE RCONSERVER_EXPORTS _WINDOWS _USRDLL)
set_property(TARGET RconServer_64 PROPERTY MSVC_RUNTIME_LIBRARY "MultiThreadedRelease")

if (CMAKE_SIZEOF_VOID_P EQUAL 8)
    message(STATUS "Compiling for 64-bit architecture")
else()
    message(FATAL_ERROR "This CMakeLists.txt is intended for 64-bit builds only.")
endif()