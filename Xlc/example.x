﻿module "test";

// add 3 numbers
func $add3 ($a i32, $b i32, $c i32) [i32] {
  i32.add (get_local $a, get_local $b);
  i32.add (get_local $c);
};

export "add3" func $add3;
