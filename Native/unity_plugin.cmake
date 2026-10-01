# Unity scene capture is part of the native asset library so the same captured
# presentation data can feed either the Windows D3D11 host or the portable
# Unity-owned renderer used by Android.
target_sources(idas3_native_assets PRIVATE src/unity_scene_capture.cpp src/unity_ui_capture.cpp)

if(ANDROID)
  set_source_files_properties(src/unity_scene_capture.cpp src/unity_ui_capture.cpp
    PROPERTIES COMPILE_DEFINITIONS "IDAS3_PORTABLE_SCENE")
else()
  set_source_files_properties(src/unity_scene_capture.cpp src/unity_ui_capture.cpp
    PROPERTIES COMPILE_DEFINITIONS "NOMINMAX;WIN32_LEAN_AND_MEAN")
endif()

# Windows obtains this list from the verified standalone executable. Android
# does not build that Win32 target, so keep an equivalent explicit source list
# for the portable Unity plugin.
if(TARGET InitialDRemake)
  get_target_property(IDAS3_APP_SOURCES InitialDRemake SOURCES)
  list(FILTER IDAS3_APP_SOURCES EXCLUDE REGEX "(^|/)main\\.cpp$")
else()
  set(IDAS3_APP_SOURCES
    src/renderer.cpp
    src/ui.cpp
    src/audio.cpp
    src/original_audio.cpp
    src/original_menu_audio.cpp
    src/frontend.cpp
    src/original_mode_menu.cpp
    src/original_choice_menu.cpp
    src/original_hud.cpp
    src/original_results.cpp
    src/car_shadow.cpp
    src/car_presentation.cpp
    src/course_scene_catalog.cpp
    src/original_number_plate.cpp
    src/original_car_body_position.cpp
    src/original_legend_menu.cpp
    src/original_battle_hud.cpp
    src/original_battle_names.cpp
    src/original_demo_presentation.cpp
    src/original_ranking_presentation.cpp
    src/original_tuning_preview.cpp)
endif()

add_library(Idas3Unity SHARED src/unity_bridge.cpp src/unity_audio_output.cpp ${IDAS3_APP_SOURCES})
target_include_directories(Idas3Unity PRIVATE src)

if(ANDROID)
  target_compile_definitions(Idas3Unity PRIVATE IDAS3_UNITY_PLUGIN IDAS3_PORTABLE_SCENE)
  # Keep the physics/native core conservative: IDAS3's simulation is tied to
  # fixed 60 Hz behavior and must not be built with fast-math transformations.
  target_compile_options(Idas3Unity PRIVATE -Wall -Wextra -fno-fast-math)
  target_link_libraries(Idas3Unity PRIVATE idas3_core idas3_native_assets idas3_original)
  target_link_options(Idas3Unity PRIVATE -Wl,--no-undefined)
  set_target_properties(idas3_core idas3_native_assets idas3_original PROPERTIES POSITION_INDEPENDENT_CODE ON)
  set_target_properties(Idas3Unity PROPERTIES
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_SOURCE_DIR}/../Assets/Plugins/Android/arm64-v8a")
elseif(WIN32)
  target_compile_definitions(Idas3Unity PRIVATE IDAS3_UNITY_PLUGIN UNICODE _UNICODE NOMINMAX WIN32_LEAN_AND_MEAN)
  target_compile_options(Idas3Unity PRIVATE /W4 /fp:strict)
  target_link_libraries(Idas3Unity PRIVATE idas3_core idas3_native_assets idas3_original d3d11 dxgi d3dcompiler gdi32 user32 shell32 xinput9_1_0 winmm)
  set_target_properties(Idas3Unity PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_SOURCE_DIR}/../Assets/Plugins/x86_64"
    MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
else()
  message(FATAL_ERROR "Idas3Unity currently supports Windows and Android hosts only")
endif()

# These exercise the D3D11 bridge and remain Windows-only. Android uses the
# synchronous Idas3Scene* portable scene ABI instead.
if(WIN32)
  add_executable(unity_scene_smoke tests/unity_scene_smoke.cpp)
  target_include_directories(unity_scene_smoke PRIVATE src)
  target_compile_definitions(unity_scene_smoke PRIVATE UNICODE _UNICODE NOMINMAX WIN32_LEAN_AND_MEAN)
  target_compile_options(unity_scene_smoke PRIVATE /W4 /fp:strict)
  target_link_libraries(unity_scene_smoke PRIVATE d3d11)

  if(EXISTS "${CMAKE_SOURCE_DIR}/tests/unity_shared_renderer_tests.cpp")
    add_executable(unity_shared_renderer_tests tests/unity_shared_renderer_tests.cpp src/renderer.cpp)
    target_compile_definitions(unity_shared_renderer_tests PRIVATE UNICODE _UNICODE NOMINMAX WIN32_LEAN_AND_MEAN)
    target_compile_options(unity_shared_renderer_tests PRIVATE /W4 /fp:strict)
    target_link_libraries(unity_shared_renderer_tests PRIVATE idas3_native_assets idas3_original d3d11 dxgi d3dcompiler user32)
    add_test(NAME unity_shared_renderer COMMAND unity_shared_renderer_tests "${CMAKE_SOURCE_DIR}")
  endif()

  if(EXISTS "${CMAKE_SOURCE_DIR}/tests/unity_bridge_smoke.cpp")
    add_executable(unity_bridge_smoke tests/unity_bridge_smoke.cpp)
    target_include_directories(unity_bridge_smoke PRIVATE src)
    target_compile_definitions(unity_bridge_smoke PRIVATE UNICODE _UNICODE NOMINMAX WIN32_LEAN_AND_MEAN)
    target_compile_options(unity_bridge_smoke PRIVATE /W4 /fp:strict)
    target_link_libraries(unity_bridge_smoke PRIVATE d3d11)
  endif()
endif()
