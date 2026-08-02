cmake_minimum_required(VERSION 3.24)

if(NOT DEFINED CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Release CACHE STRING "Build type")
endif()

project(RconServer_32 LANGUAGES CXX)

if(NOT CMAKE_SIZEOF_VOID_P EQUAL 4)
    message(FATAL_ERROR "rconserver-galaxy must be built for x86")
endif()

option(RCONSERVER_BUILD_DIRECT_TESTS "Build the direct transport test suite" ON)
set(BF2_DIRECT_BUILD_TESTS OFF CACHE BOOL "" FORCE)
set(BF2_DIRECT_DYNAMIC_CRT ON CACHE BOOL "" FORCE)
add_subdirectory(
    "${CMAKE_CURRENT_LIST_DIR}/external/transport-common"
    "${CMAKE_CURRENT_BINARY_DIR}/transport-common")
add_subdirectory(
    "${CMAKE_CURRENT_LIST_DIR}/external/minhook"
    "${CMAKE_CURRENT_BINARY_DIR}/minhook")
set_property(TARGET minhook PROPERTY
    MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL")

set(SOURCES
    dllmain.cpp
    bf2server.cpp
    md5.cpp
    Logger.cpp
    RconClient.cpp
    RconServer.cpp
    direct_transport/direct_transport_hooks.cpp
    direct_transport/direct_transport_logging.cpp
    direct_transport/direct_transport_policy.cpp
    direct_transport/direct_transport_profile.cpp
    direct_transport/direct_transport_server.cpp
    direct_transport/galaxy_peer_endpoint.cpp
)

add_library(RconServer_32 SHARED ${SOURCES})
target_link_libraries(RconServer_32 PRIVATE
    Ws2_32
    bf2direct_common
    minhook
)
target_compile_definitions(RconServer_32 PRIVATE
    WIN32_LEAN_AND_MEAN
    NOMINMAX
    _CRT_SECURE_NO_WARNINGS
)
target_include_directories(RconServer_32 PRIVATE
    "${CMAKE_CURRENT_SOURCE_DIR}"
)

if(MSVC)
    set_property(TARGET RconServer_32 PROPERTY
        MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL")
    target_compile_options(RconServer_32 PRIVATE /W4 /permissive-)
endif()

set_target_properties(RconServer_32 PROPERTIES
    CXX_STANDARD 20
    CXX_STANDARD_REQUIRED YES
    CXX_EXTENSIONS NO
    PREFIX ""
)

if(RCONSERVER_BUILD_DIRECT_TESTS)
    enable_testing()

    function(rconserver_direct_test_defaults target)
        target_include_directories(${target} PRIVATE
            "${CMAKE_CURRENT_SOURCE_DIR}"
            "${CMAKE_CURRENT_LIST_DIR}/external/transport-common/include")
        target_compile_definitions(${target} PRIVATE
            WIN32_LEAN_AND_MEAN NOMINMAX)
        set_target_properties(${target} PROPERTIES
            CXX_STANDARD 20
            CXX_STANDARD_REQUIRED YES
            CXX_EXTENSIONS NO)
        if(MSVC)
            set_property(TARGET ${target} PROPERTY
                MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL")
            target_compile_options(${target} PRIVATE /W4 /permissive-)
        endif()
    endfunction()

    set(BF2_DIRECT_GOG_TEST_EXE "" CACHE FILEPATH
        "Exact GOG executable used for direct transport profile verification")
    if(EXISTS "${BF2_DIRECT_GOG_TEST_EXE}")
        add_executable(bf2direct-server-profile-test
            direct_transport/direct_transport_profile.cpp
            direct_transport/direct_transport_profile_test.cpp)
        rconserver_direct_test_defaults(bf2direct-server-profile-test)
        target_link_libraries(bf2direct-server-profile-test PRIVATE
            bf2direct_common)
        add_test(NAME bf2direct_server_profile
            COMMAND bf2direct-server-profile-test
                "${BF2_DIRECT_GOG_TEST_EXE}")
    elseif(BF2_DIRECT_GOG_TEST_EXE)
        message(WARNING
            "GOG direct profile executable not found: ${BF2_DIRECT_GOG_TEST_EXE}")
    endif()

    add_executable(bf2direct-server-abi-test
        Logger.cpp
        direct_transport/direct_transport_abi_test.cpp
        direct_transport/direct_transport_hooks.cpp
        direct_transport/direct_transport_logging.cpp
        direct_transport/direct_transport_policy.cpp
        direct_transport/direct_transport_profile.cpp
        direct_transport/direct_transport_server.cpp
        direct_transport/galaxy_peer_endpoint.cpp)
    rconserver_direct_test_defaults(bf2direct-server-abi-test)
    target_compile_definitions(bf2direct-server-abi-test PRIVATE
        BF2_DIRECT_ABI_TEST=1)
    target_link_libraries(bf2direct-server-abi-test PRIVATE
        bf2direct_common minhook)
    add_test(NAME bf2direct_server_abi
        COMMAND bf2direct-server-abi-test)

    add_executable(bf2direct-server-policy-test
        direct_transport/direct_transport_policy.cpp
        direct_transport/direct_transport_policy_test.cpp)
    rconserver_direct_test_defaults(bf2direct-server-policy-test)
    add_test(NAME bf2direct_server_policy
        COMMAND bf2direct-server-policy-test)

    add_executable(bf2direct-server-logging-test
        Logger.cpp
        direct_transport/direct_transport_logging.cpp
        direct_transport/direct_transport_logging_test.cpp
        direct_transport/direct_transport_policy.cpp)
    rconserver_direct_test_defaults(bf2direct-server-logging-test)
    target_link_libraries(bf2direct-server-logging-test PRIVATE
        bf2direct_common)
    add_test(NAME bf2direct_server_logging
        COMMAND bf2direct-server-logging-test)

    add_executable(bf2direct-server-state-test
        Logger.cpp
        direct_transport/direct_transport_hooks.cpp
        direct_transport/direct_transport_logging.cpp
        direct_transport/direct_transport_policy.cpp
        direct_transport/direct_transport_profile.cpp
        direct_transport/direct_transport_server.cpp
        direct_transport/direct_transport_server_test.cpp
        direct_transport/galaxy_peer_endpoint.cpp)
    rconserver_direct_test_defaults(bf2direct-server-state-test)
    target_compile_definitions(bf2direct-server-state-test PRIVATE
        BF2_DIRECT_SERVER_TEST=1)
    target_link_libraries(bf2direct-server-state-test PRIVATE
        bf2direct_common minhook)
    add_test(NAME bf2direct_server_state
        COMMAND bf2direct-server-state-test)
endif()

set(RCONSERVER_STAGE_DIR "${CMAKE_CURRENT_SOURCE_DIR}/out")

add_custom_command(TARGET RconServer_32 POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E make_directory "${RCONSERVER_STAGE_DIR}"
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
        "$<TARGET_FILE:RconServer_32>"
        "${RCONSERVER_STAGE_DIR}/RconServer_32.dll"
    COMMENT "Staging RconServer_32.dll for SWBF2Admin"
)
