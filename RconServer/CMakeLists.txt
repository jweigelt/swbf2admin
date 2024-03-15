cmake_minimum_required(VERSION 3.10)
project(rconserver)

set(CMAKE_BUILD_TYPE Release)
set(CMAKE_GENERATOR_PLATFORM amd64)

set(SOURCES
        dllmain.cpp
        bf2server.cpp
        Logger.cpp
        )

add_library(rconserver SHARED ${SOURCES})
target_compile_definitions(rconserver PRIVATE RCONSERVER_EXPORTS _WINDOWS _USRDLL)
set_property(TARGET rconserver PROPERTY MSVC_RUNTIME_LIBRARY "MultiThreadedRelease")

if (CMAKE_SIZEOF_VOID_P EQUAL 8)
    message(STATUS "Compiling for 64-bit architecture")
else()
    message(FATAL_ERROR "This CMakeLists.txt is intended for 64-bit builds only.")
endif()