﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using micfort.GHL.Math2;

namespace Project1
{
	class Game: GameWindow
	{
		private int N;
		private float dt, d;
		private bool dsim;
		private bool dump_frames;
		private int frame_number;

		// static Particle *pList;
		private List<Particle> particles;

		private int win_id;
		private int win_x, win_y;
		private int[] mouse_down;
		private int[] mouse_release;
		private int[] mouse_shiftclick;
		private int omx, omy, mx, my;
		private int hmx, hmy;

        private List<Force> forces;
        private List<Constraint> contrains;


		/*
		----------------------------------------------------------------------
		free/clear/allocate simulation data
		----------------------------------------------------------------------
		*/

		private void ClearData()
		{
			particles.ForEach(x => x.reset());
		}

		private void InitSystem()
		{
			float dist = 0.2f;
            HyperPoint<float> center = new HyperPoint<float>(0.0f, 0.0f);
            HyperPoint<float> offset = new HyperPoint<float>(dist, 0.0f);

			particles = new List<Particle>();

			particles.Add(new Particle(center + offset * 1, 1.0f));
            particles.Add(new Particle(center + offset * 2, 1.0f));
            particles.Add(new Particle(center + offset * 3, 1.0f));

            forces = new List<Force>();
            forces.Add(new SpringForce(particles[0], particles[1], dist * 2, 1.0f, 1.0f));
            forces.Add(new SpringForce(particles[1], particles[2], dist * 2, 1.0f, 1.0f));
            //forces.Add(new SpringForce(particles[2], particles[0], dist * 3, 1.0f, 1.0f));
            forces.Add(new GravityForce(particles[0]));
            forces.Add(new GravityForce(particles[1]));
            forces.Add(new GravityForce(particles[2]));

            contrains = new List<Constraint>();
            contrains.Add(new RodConstraint(particles[0], particles[1], dist));
            contrains.Add(new CircularWireConstraint(particles[0], center, dist * 1));
            //contrains.Add(new CircularWireConstraint(particles[1], center, dist * 2));
            //contrains.Add(new CircularWireConstraint(particles[2], center, dist * 3));
		}

		/*
		----------------------------------------------------------------------
		OpenGL specific drawing routines
		----------------------------------------------------------------------
		*/

		private void PreDisplay()
		{
			GL.Viewport(0, 0, win_x, win_y);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			GL.Ortho(-1.0, 1.0, -1.0, 1.0, -1, 1);
			GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
			GL.Clear(ClearBufferMask.ColorBufferBit);
		}

		private void PostDisplay()
		{
			// Write frames if necessary.
			if (dump_frames)
			{
				const int FrameInterval = 4;
				if((frame_number % FrameInterval) == 0)
				{
					using(Bitmap bmp = new Bitmap(Width, Height))
					{
						System.Drawing.Imaging.BitmapData data =
							bmp.LockBits(this.ClientRectangle, System.Drawing.Imaging.ImageLockMode.WriteOnly,
										 System.Drawing.Imaging.PixelFormat.Format24bppRgb);
						GL.ReadPixels(0, 0, Width, Height, PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
						bmp.UnlockBits(data);

						bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

						if (!Directory.Exists("snapshots"))
							Directory.CreateDirectory("snapshots");

						string filename = string.Format("snapshots/img{0}.png", Convert.ToSingle(frame_number)/FrameInterval);
						bmp.Save(filename);
						Console.Out.WriteLine("Output snapshot: {0}", Convert.ToSingle(frame_number) / FrameInterval);
					}
				}
			}
			frame_number++;

			SwapBuffers();
		}

		private void DrawParticles()
		{
			particles.ForEach(x => x.draw());
		}

		private void DrawForces()
        {
            forces.ForEach(f => f.Draw());
		}

		private void DrawConstraints()
        {
            contrains.ForEach(c => c.Draw());
		}
		
		/*
		----------------------------------------------------------------------
		relates mouse movements to tinker toy construction
		----------------------------------------------------------------------
		*/
        
		/*
		----------------------------------------------------------------------
		callback routines
		----------------------------------------------------------------------
		*/
        
		private void OnLoad(object sender, EventArgs eventArgs)
		{
			// setup settings, load textures, sounds
			VSync = VSyncMode.On;

			GL.Enable(EnableCap.LineSmooth); 
			GL.Enable(EnableCap.PolygonSmooth);
		}

		private void OnResize(object sender, EventArgs eventArgs)
		{
			GL.Viewport(0, 0, Width, Height);
			win_x = Width;
			win_y = Height;
		}

		private void OnRenderFrame(object sender, FrameEventArgs frameEventArgs)
		{
			PreDisplay();

			DrawForces();
			DrawConstraints();
			DrawParticles();
			
			PostDisplay();

			// render graphics
			//GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			//GL.MatrixMode(MatrixMode.Projection);
			//GL.LoadIdentity();
			//GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 4.0);

			//GL.Begin(PrimitiveType.Triangles);

			//GL.Color3(Color.MidnightBlue);
			//GL.Vertex2(-1.0f, 1.0f);
			//GL.Color3(Color.SpringGreen);
			//GL.Vertex2(0.0f, -1.0f);
			//GL.Color3(Color.Ivory);
			//GL.Vertex2(1.0f, 1.0f);

			//GL.End();

			//SwapBuffers();
		}

		private void OnUpdateFrame(object sender, FrameEventArgs frameEventArgs)
		{
			if(dsim)
			{
				Solver.SimulationStep(particles, forces, contrains, dt);
			}
			else
			{
				//todo reset
			}

			// add game logic, input handling
			if (Keyboard[Key.Escape])
			{
				Exit();
			}
		}

		private void OnKeyUp(object sender, KeyboardKeyEventArgs keyboardKeyEventArgs)
		{
		}

		private void OnKeyDown(object sender, KeyboardKeyEventArgs keyboardKeyEventArgs)
		{
			switch (keyboardKeyEventArgs.Key)
			{
				case Key.C:
					ClearData();
					break;

				case Key.D:
					dump_frames = !dump_frames;
					break;

				case Key.Q:
					Exit();
					break;

				case Key.Space:
					dsim = !dsim;
					break;
			}
		}
        
		public Game(int n, float dt, float d)
		{
			this.N = n;
			this.dt = dt;
			this.d = d;

			dsim = false;
			dump_frames = false;
			frame_number = 0;

			InitSystem();

			this.Load += OnLoad;
			this.Resize += OnResize;
			this.UpdateFrame += OnUpdateFrame;
			this.RenderFrame += OnRenderFrame;
			this.KeyDown += OnKeyDown;
			this.KeyUp += OnKeyUp;
		}
	}
}