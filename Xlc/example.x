
/* example module */
module #hello

func @console.log ($msg string) !console@log;

// silly add function
func $add ($a i32, $b i32) i32 {
  i32.add ($a, $b);
  i32.add ($last);
}

var $last i32 := 0;

func $fib ($n i32) i32 {
  if ($n >= 0 && $n <= 1) {
    return 1;
  } else {
    call $fib ($n - 2);
    call $fib ($n - 1);
    i32.add
  }
  setg $last (dup);
}
