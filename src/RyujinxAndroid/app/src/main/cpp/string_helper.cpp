//
// Created by Emmanuel Hansen on 10/30/2023.
//

#include "string_helper.h"

long string_helper::store_cstring(const char *cstr) {
    auto id = ++current_id;
    _map.insert({id, cstr});
    return id;
}

long string_helper::store_string(const string& str) {
    auto id = ++current_id;
    _map.insert({id, str});
    return id;
}

string string_helper::get_stored(long id) {
    auto str = _map[id];
    _map.erase(id);

    return str;
}
