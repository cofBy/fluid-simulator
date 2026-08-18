<img width="894" height="887" alt="Screenshot 2026-08-14 181946" src="https://github.com/user-attachments/assets/b66a1987-9386-4ded-8d20-b2ea45149737" />
# performance
it simulates a 14400 particle with 100fps (in the editor which is much slower)<br/> on my device (AMD Ryzen 5 4500 6-Core Processor, NVIDIA GeForce GTX 1660 Ti) <br/>
I also recommend you test the performance on your device from [it's itch.io page](https://cof99.itch.io/fluid-simulator)

# using instructions
just copy the repo into your unity project and make a quad in front of the camera and give it the particlesfullscreen material

## how it works
it all runs on the gpu using a compute shader and here's the logic behind it all : <br/>
it just calculates the density (how much particles are in a small radius) and moves particles to the places where the density = the target density <br/>
and also, every particle tries to have a similar velocity to the neighbouring particles' velocities which is called viscosity
then it every particle draws the density around it on a texture which is then rendered custom shader
<img width="894" height="887" alt="Screenshot 2026-08-14 181946" src="https://github.com/user-attachments/assets/83da135b-055a-4e27-be00-902fcbc3c406" />
(the brightest places are the most dense)
