// /cmds/testlocal.c
// Test command for local variable support

void main(string args) {
    int i;
    int sum;
    sum = 0;
    for (i = 0; i < 5; i++) {
        sum = sum + i;
    }
    write("Sum of 0..4: " + sum);

    string msg;
    msg = "Local variables work!";
    write(msg);
}
