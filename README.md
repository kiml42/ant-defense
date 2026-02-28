# ant-defense

## Project Overview

**Ant Defense** is a Unity tower-defense game where players defend a picnic from attacking ants. Ants autonomously forage for food via pheromone-like scent trails; players place defensive structures (turrets, walls, traps) to stop them.

## Playing
There are various versions included in the CompressedBuilds folder that can be run on Windows.

The aim of the game is to stop the ants from stealing the food from your picnic by placing walls and turrets to defend it.
The ants will automatically forage for food, and leave trails to encourage more ants to come to any large source of food.
As well as your picnic, there are berry bushes that will provide food for the ants, so their numbers will increase over time, even if you're defending your picnic well.

### Controls
#### Camera
Right click and drag to move the camera
Scroll to zoom in and out

#### Building
Click an object in the quick bar, or place the appropriate number (1 for the one on the left) to start building an object.

1. Wall Node - When placed immediately starts placing another wall node connected to the first by a wall.

2. Turret - Can be placed on an existing wall node, or on the ground in which case it creates a wall node

3. Berry - Can be placed on the ground to feed the ants, I don't know why you'd want to, but you can!

4. Flip trap - Can be placed on the ground. It'll throw any ants that step on it (if it triggers correctly). You can hold down the left click to choose the orientation while placing it. It doesn't work well because the ants don't have ground checks, so they just carry on moving in the air! Also, it doesn't really fit with how the whole game works, so it'll probably be removed later.