(module 
(import "console" "log" (func $fnc_log (param $lcl_msg i32) (result ) ))
(global $gbl_what? (mut i32) i32.const 42 )
(func $fnc_add-what? (param $lcl_a i32) (result i32) get_local $lcl_a get_global $gbl_what? i32.add i32.const 10 i32.add get_global $gbl_what? call $fnc_log )
(func $fnc_fib (param $lcl_n i32) (result i32) get_local $lcl_n i32.const 1 i32.le_u if (result i32) i32.const 1 else get_local $lcl_n i32.const 1 i32.sub call $fnc_fib get_local $lcl_n i32.const 2 i32.sub call $fnc_fib i32.add end )
(export "add_what" (func $fnc_add-what?))
)
