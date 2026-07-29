cmake_minimum_required(VERSION 3.15)

if(NOT DEFINED CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Release CACHE STRING "Build type")
endif()

project(RconServer_64 LANGUAGES CXX)

if(NOT CMAKE_SIZEOF_VOID_P EQUAL 8)
    message(FATAL_ERROR "rconserver-aspyr must be built for x64")
endif()

set(SOURCES
    dllmain.cpp
    bf2server.cpp
    PatchEngine.cpp
    md5.cpp
    Logger.cpp
    RconClient.cpp
    RconServer.cpp
)

add_library(RconServer_64 SHARED ${SOURCES})
target_compile_features(RconServer_64 PRIVATE cxx_std_20)
target_compile_definitions(RconServer_64 PRIVATE
    WIN32_LEAN_AND_MEAN
    NOMINMAX
    _CRT_SECURE_NO_WARNINGS
)

if(MSVC)
    set_property(TARGET RconServer_64 PROPERTY
        MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL")
    target_compile_options(RconServer_64 PRIVATE /W4 /permissive-)
endif()

set_target_properties(RconServer_64 PROPERTIES PREFIX "")

add_custom_command(TARGET RconServer_64 POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E make_directory "${CMAKE_CURRENT_SOURCE_DIR}/out"
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
        "$<TARGET_FILE:RconServer_64>"
        "${CMAKE_CURRENT_SOURCE_DIR}/out/RconServer_64.dll"
    COMMENT "Staging RconServer_64.dll for SWBF2Admin"
)
