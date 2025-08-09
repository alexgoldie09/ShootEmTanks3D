# Shoot Em' Tanks 3D (Custom Math & Physics Engine)

This is a 3D tank game prototype built in Unity that runs entirely on a **custom mathematics and physics engine**, replacing Unity's built-in transform and physics systems. The project uses a bespoke stack of linear algebra operations (vectors, matrices, quaternions) and a hand-crafted physics simulation to handle movement, rotation, collisions, and simple rigid body dynamics.

---

## 🚀 Project Goals

- Build a functional 3D tank gameplay loop with movement, shooting, scoring, and win/lose states.
- Replace Unity’s default transform and physics pipeline with custom matrix/quaternion math and collision handling.
- Simulate rigid body movement, collisions, and projectile motion without `Rigidbody` or Unity colliders.
- Visualize and debug positions, collisions, and projectile trajectories via Gizmos.
- Create a reusable math + physics foundation for future 3D games.

---

## 🛠 Features

- ✅ **Custom Math Stack**: immutable `Coords`, `Matrix`, and `CustomQuaternion` types with `MathEngine` helpers.
- ✅ **Manual Physics Simulation**: position, velocity, acceleration, gravity, and impulse-based movement.
- ✅ **Custom Collision System**:
  - Sphere ↔ Sphere
  - AABB ↔ AABB
  - AABB ↔ Sphere
  - Point hits (e.g., projectile impacts)
- ✅ **Player Tank Movement** using transformation matrices and quaternions for yaw rotation and forward/back translation.
- ✅ **Shell Firing System** with gravity, collision checks, and projectile orientation.
- ✅ **Trajectory Visualiser** showing predicted projectile arc and impact point.
- ✅ **Enemy AI**: patrol waypoints, chase player on line-of-sight, take damage, and award score.
- ✅ **Scoring & Progress**: crate delivery, enemy defeats, win/lose states.
- ✅ **UI System**: score display, crate progress, win/lose overlays, transient messages.
- ✅ **Main Menu** with scene loading and quit functionality.

---

## 📐 Powered by Custom Math Stack

Includes:

- Vector operations: dot, cross, normalize, magnitude, reflection, lerp
- Quaternion math: axis-angle constructors, multiplication, rotation matrices
- Matrix transforms: translation, scale, rotation (XYZ & quaternion-based), reflection, shear
- Transform extraction (`ExtractPosition`, `ExtractScale`)
- Custom raycasting against sphere and AABB geometry

---

## 🎮 Gameplay Overview

1. **Movement** — Drive forward/back and rotate the tank using custom transform math (yaw rotation only).
2. **Aiming & Firing** — Adjust shell force with the mouse wheel; fire shells with spacebar.
3. **Projectile Simulation** — Shells follow a gravity-affected path with collision detection; a trajectory preview shows the impact point.
4. **Combat & Interaction** — Hit enemies to defeat them; deliver crates to goals to score points.
5. **Win/Lose States** — Reach the crate goal to win; get destroyed by enemies to lose.
6. **Restart or Menu** — UI buttons restart the game or return to main menu.

All movement, aiming, rotation, collisions, and projectile physics are driven entirely by the **custom math + physics system**—no Unity `Rigidbody` or colliders.

---

## 🎯 Controls

- W/S             Move forward/back
- A/D             Turn (yaw)
- Mouse Scroll    Adjust shell force
- Space           Fire shell

---

## 📄 License

This project is open-source and built for educational and prototyping purposes.
