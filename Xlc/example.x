module "test";

// add 3 numbers
fn $add3 ($a i32, $b i32, $c i32) [i32] {
  i32.add (getl $a, getl $b);
  i32.add (getl $c);
}

export "add3" .add3;
