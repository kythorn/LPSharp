// /cmds/std/help.c
// In-game help system - displays help topics and command documentation

string *get_help_files(string dir) {
    string *files;
    string *result;
    int i;
    int count;

    files = get_dir(dir);
    if (!files || sizeof(files) == 0) {
        return ({});
    }

    result = ({});
    count = sizeof(files);
    i = 0;
    while (i < count) {
        // Skip directories and hidden files
        if (files[i] != "." && files[i] != "..") {
            result = result + ({ files[i] });
        }
        i = i + 1;
    }
    return result;
}

void show_index() {
    string *topics;
    string *commands;
    string output;
    int i;
    int count;

    output = "";
    output = output + "===============================================================================\n";
    output = output + "                         LPMud Revival - Help System\n";
    output = output + "===============================================================================\n\n";
    output = output + "Type 'help <topic>' to learn more about any topic listed below.\n\n";

    // Show topics
    topics = get_help_files("/help/topics");
    if (sizeof(topics) > 0) {
        output = output + "TOPICS\n";
        output = output + "------\n";
        count = sizeof(topics);
        i = 0;
        while (i < count) {
            output = output + "  " + topics[i] + "\n";
            i = i + 1;
        }
        output = output + "\n";
    }

    // Show commands
    commands = get_help_files("/help/commands");
    if (sizeof(commands) > 0) {
        output = output + "COMMANDS\n";
        output = output + "--------\n";
        count = sizeof(commands);
        i = 0;
        while (i < count) {
            output = output + "  " + commands[i] + "\n";
            i = i + 1;
        }
        output = output + "\n";
    }

    output = output + "===============================================================================\n";
    write(output);
}

void main(string args) {
    string topic;
    string content;
    string path;

    // Show dynamic index if no argument
    if (args == "" || args == 0) {
        show_index();
        return;
    }

    topic = lower_case(args);

    // Try to find help file in order: commands, topics, then root help
    path = "/help/commands/" + topic;
    content = read_file(path);

    if (content == 0 || content == "") {
        path = "/help/topics/" + topic;
        content = read_file(path);
    }

    if (content == 0 || content == "") {
        path = "/help/" + topic;
        content = read_file(path);
    }

    if (content == 0 || content == "") {
        write("No help available for '" + topic + "'.\n");
        write("Type 'help' for a list of topics, or 'help <topic>' for specific help.\n");
        return;
    }

    write(content);
}
