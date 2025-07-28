# Shoot Em' Tanks 3D (Custom Math & Physics Engine)

This is a 3D tank game project built in Unity that leverages a **custom mathematics and physics engine**, designed to replace Unity's built-in physics system. The project uses a bespoke stack of linear algebra operations (vectors, matrices, quaternions) and a hand-crafted physics simulation to handle movement, collisions, and rigid body dynamics.

## 🚀 Project Goals

- Build a functional 3D tank game prototype with accurate movement and shooting mechanics.
- Replace Unity’s default transform and physics pipeline with custom matrix/quaternion math and manual collision handling.
- Visualize and debug movement, collisions, and physics behavior using Gizmos and editor tools.
- Create a reusable math-physics foundation for future 3D gameplay systems.

## 🛠 Features

- ✅ Fully custom vector, matrix, and quaternion math libraries.
- ✅ Custom rigid body physics including velocity, acceleration, and damping.
- ✅ Manual collision detection (sphere vs. sphere, AABB, etc.) using your own collider system.
- ✅ Tank movement using transformation matrices (no reliance on Unity physics).
- ✅ Shell firing system using projectile motion.
- ✅ OnDrawGizmos-based visualization for debugging physics and collision data.

## 📦 Project Structure

- `MathEngine/`: Contains core math types (CustomVector3, CustomMatrix4x4, CustomQuaternion).
- `PhysicsEngine/`: Core physics scripts including motion integrators, collisions, and custom rigidbodies.
- `Tank/`: Game objects for player tank, turret, and shell behavior.
- `Core/`: Utility and simulation management scripts.

## 📐 Powered by Custom Math Stack

It includes:

- Quaternion-to-matrix transformations
- Custom vector math operations
- RigidBody simulation loops
- Collision detection using geometric math

## 🎮 Gameplay Overview

You control a tank in a 3D space. The tank uses the custom physics engine for movement and aiming. You can rotate the turret, move the tank using physics impulses, and fire shells that follow simulated projectile trajectories. All movement, rotation, and collisions are driven by the custom systems—no Rigidbody or Unity physics components involved.

---

## 📄 License

This project is open-source and built for educational and prototyping purposes.
