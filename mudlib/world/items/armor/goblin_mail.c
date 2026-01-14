// Goblin Mail - Tier 3 armor
// Dropped by hobgoblins

inherit "/std/armor";

void create() {
    ::create();
    set_name("mail");
    add_id("chainmail");
    add_id("goblin mail");
    add_id("goblin chainmail");
    set_short("crude goblin chainmail");
    set_mass(15);
    set_armor_class(4);
    set_slot("torso");
    set_weight_category("medium");
}
