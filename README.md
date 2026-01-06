# DOTS-Swarm-Simulation
A high-performance swarm simulation built with Unity's Data-Oriented Technology Stack (DOTS), Entity Component System (ECS), Burst Compiler, and Job System. This project demonstrates advanced Unity optimization techniques capable of simulating 10,000+ entities at 60 FPS.

Key Features:

🐟 10,000+ agents with realistic flocking behavior

🦈 Predator-prey dynamics with intelligent AI

⚡ High-performance using DOTS/ECS

🎯 Spatial partitioning for efficient neighbor detection

🚀 Instanced rendering for optimal GPU usage

⚙️ Real-time parameter tuning


## Demonstration GIF:
![Демонстрация видео](https://github.com/IdarDzhamzarov/DOTS-Swarm-Simulation/blob/main/DOTSSwarmSimulation-SampleScene-WindowsMacLinux-Unity6.26000.2.10f1_DX11_2026-01-0522-56-38-ezgif.com-optimize.gif)

## How it works?
This simulation uses Unity's Entity Component System (ECS) instead of traditional GameObjects. Each agent is a lightweight entity with data components, enabling massive parallelism and optimal memory layout. Systems process thousands of entities simultaneously using Burst-compiled jobs.
