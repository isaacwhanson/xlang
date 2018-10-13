(module "example"
(func $add3 (param $a i32) (param $b i32) (param $c i32) (result i32) get_local $a get_local $b i32.add get_local $c i32.add )
(export "add3" (func $add3) )
)
