cmake_minimum_required(VERSION 3.10)
project(RconServer_32)

set(CMAKE_BUILD_TYPE Release)
set(CMAKE_GENERATOR_PLATFORM x86)

set(SOURCES
        dllmain.cpp
        bf2server.cpp
        md5.cpp
        Logger.cpp
        RconClient.cpp
        RconServer.cpp
        )

add_library(RconServer_32 SHARED ${SOURCES})
target_compile_definitions(RconServer_32 PRIVATE RCONSERVER_EXPORTS _WINDOWS _USRDLL)
set_property(TARGET RconServer_32 PROPERTY MSVC_RUNTIME_LIBRARY "MultiThreadedRelease")