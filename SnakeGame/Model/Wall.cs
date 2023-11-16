﻿using System;
using SnakeGame;
using System.Text.Json.Serialization;
namespace Model
{
	public class Wall
	{
		public int wall;
		public Vector2D p1;
		public Vector2D p2;

		[JsonConstructor]
		public Wall(int wall, Vector2D p1, Vector2D p2)
		{
			this.wall = wall;
			this.p1 = p1;
			this.p2 = p2;
		}
	}
}

