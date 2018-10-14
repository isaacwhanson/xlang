(module 
(global $gbl_what? (mut i32) i32.const 42 )
(func $fnc_add-what? (param $a i32) (result i32) get_local $a get_global $gbl_what? i32.add i32.const 10 i32.add )
(export "add_what" (func $fnc_add-what?))
)
