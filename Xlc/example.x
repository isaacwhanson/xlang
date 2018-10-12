module "test"

func $add ($a i32, $b i32) i32 {
  i32.add ($a, $b);
}

func $fib ($n i32) i32 {
  if ($n <= 1) {
    return 1;
  } else {
    .$fib ($n - 1);
    .$fib ($n - 2);
    i32.add;
  }
}

export "fib" (func $fib)
export "add" (func $add)

fn @add ($a i32, $b i32) [i32] {
  // 42; set $c;
  $c := 42;
  // get $a; get $b; i32.add; set $d;
  // set $d (i32.add ($a, $b));
  $d := $a + $b; //-> set $d ($a, $b, i32.add)
  // get $a; get $b; i32.add;
  // ($a, $b); i32.add;
  i32.add ($a, $b);
  // get $c; i32.add;
  i32.add $c; 
}

fn @weird ($a i32) [i32] {
  $c := 2;
  .@add ($a, 2);
  i32.mul 4;
  i32.div ($c - 1); // i32.div (i32.sub ($c, 1));
}
