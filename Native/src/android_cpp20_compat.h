#pragma once

// Android NDK r25 ships a libc++ snapshot with partial C++20 library support.
// Clang already provides __builtin_bit_cast, but that libc++ does not expose
// std::bit_cast. Supply the standard operation only when the library feature
// macro is absent so newer NDKs keep their own implementation.
#include <bit>
#include <type_traits>
#include <cstring>

#if !defined(__cpp_lib_bit_cast) || __cpp_lib_bit_cast < 201806L
namespace std {
template<class To,class From,
    enable_if_t<sizeof(To)==sizeof(From) &&
                is_trivially_copyable_v<To> &&
                is_trivially_copyable_v<From>,int> = 0>
#if defined(__has_builtin) && __has_builtin(__builtin_bit_cast)
constexpr To bit_cast(const From& source) noexcept { return __builtin_bit_cast(To,source); }
#else
To bit_cast(const From& source) noexcept {
    To destination{};
    std::memcpy(&destination,&source,sizeof(To));
    return destination;
}
#endif
}
#endif
