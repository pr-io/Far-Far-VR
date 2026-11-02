#include "../Engine/FarFarWestMenu.hpp"
#include <cassert>
#include <iostream>
int main(){
 farfarvr::MenuStartup s;
 assert(!s.update(0,false,false));assert(!s.update(100,true,false));assert(!s.update(599,true,false));
 assert(!s.update(600,false,false));assert(!s.update(700,true,false));assert(s.update(1200,true,false));
 assert(s.announced);assert(!s.update(1800,true,false));assert(!s.update(2000,false,false));assert(!s.update(3000,true,false));
 farfarvr::MenuStartup explicit_open;assert(!explicit_open.update(10,true,true));assert(!explicit_open.update(510,true,true));assert(explicit_open.announced);
 using farfarvr::valid_menu_command;
 assert(valid_menu_command(10,10,1000,1000,true));assert(valid_menu_command(10,10,1000,6000,true));
 assert(!valid_menu_command(10,11,1000,1000,true));assert(!valid_menu_command(10,10,1000,1000,false));
 assert(!valid_menu_command(10,10,1000,6001,true));assert(!valid_menu_command(10,10,1001,1000,true));
 std::cout<<"Menu startup readiness, single welcome, explicit-open preservation and command isolation passed\n";
}
