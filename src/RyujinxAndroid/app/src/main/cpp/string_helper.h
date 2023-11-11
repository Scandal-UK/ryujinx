//
// Created by Emmanuel Hansen on 10/30/2023.
//

#ifndef RYUJINXANDROID_STRING_HELPER_H
#define RYUJINXANDROID_STRING_HELPER_H

#include <string>
#include <unordered_map>
using namespace std;
class string_helper {
public:
    long store_cstring(const char * cstr);
    long store_string(const string& str);

    string get_stored(long id);

    string_helper(){
        _map = unordered_map<long,string>();
        current_id = 0;
    }

private:
    unordered_map<long, string> _map;
    long current_id;
};


#endif //RYUJINXANDROID_STRING_HELPER_H
