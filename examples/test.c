// A simple LPC test file
inherit "/std/object";

int counter;

void create() {
    ::create();
    counter = 0;
}

int add(int a, int b) {
    return a + b;
}

void increment() {
    counter++;
}

string greet(string name) {
    if (name == "") {
        return "Hello, stranger!";
    } else {
        return "Hello, " + name + "!";
    }
}
