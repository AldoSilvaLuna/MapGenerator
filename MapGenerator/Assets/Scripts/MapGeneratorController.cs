﻿using UnityEngine; 
using System.Collections;
using System.Collections.Generic;
using System;

public class MapGeneratorController : MonoBehaviour {
	public int width;
	public int height;
	public string seed;
	public bool useRandomSeed;

	[Range(0,100)]
	public int randomFillPercent;

	int[,] map;

	void Start(){			//Funcion que se llama al inicio del programa
		GenerateMap ();
	}

	void Update(){			//Funcion que se llama en cada frame
		if (Input.GetMouseButtonDown(0)) {
			GenerateMap ();
		}
	}
	void GenerateMap()
	{
		map = new int[width, height];
		RandomFillMap ();
		for (int i = 0; i < 5; i++) {
			SmoothMap ();
		}

		ProcessMap ();

		int borderSize = 1;		//Con esta variable se controla el numero de cuadros para la orilla
		int[,] borderedMap = new int[width + borderSize * 2, height + borderSize * 2];
		for (int x = 0; x < borderedMap.GetLength (0); x++) {
			for (int y = 0; y < borderedMap.GetLength (1); y++) {
				if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize) {
					borderedMap[x,y] = map [x - borderSize, y - borderSize];
				} else {
					borderedMap[x,y] = 1;
				}
			}
		}

		MeshGenerator meshGen = GetComponent<MeshGenerator> ();
		meshGen.GenerateMesh (borderedMap, 1);
	}
	/*
		Se encarga de eliminar cuartos que no son suficientemente grandes
	*/
	void ProcessMap(){
		List<List<Coord>> wallRegions = GetRegions (1);
		int wallThresholdSize = 50;						// Minimum tile size of the regions, if not its removed
		foreach (List<Coord> wallRegion in wallRegions) {
			if (wallRegion.Count < wallThresholdSize) {
				foreach (Coord tile in wallRegion) {
					map [tile.tileX, tile.tileY] = 0;
				}
			}
		}

		List<List<Coord>> roomRegions = GetRegions (0);
		int roomThresholdSize = 50;					// Minimum tile size of the regions, if not its removed
		List<Room> survivingRooms = new List<Room>();

		foreach (List<Coord> roomRegion in roomRegions) {
			if (roomRegion.Count < roomThresholdSize) {
				foreach (Coord tile in roomRegion) {
					map [tile.tileX, tile.tileY] = 1;
				}
			} else {
				survivingRooms.Add (new Room(roomRegion, map));
			}
		}
		survivingRooms.Sort ();
		survivingRooms [0].isMainRoom = true;
		survivingRooms [0].isAccessibleFromMainRoom = true;

		ConnectClosestRooms (survivingRooms);
	}

	void ConnectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false){
		List<Room> roomListA = new List<Room> ();		// Are not accesible from main room
		List<Room> roomListB = new List<Room> ();		// Are Accessible From Main Room

		if (forceAccessibilityFromMainRoom) {
			foreach (Room room in allRooms) {
				if (room.isAccessibleFromMainRoom) {
					roomListB.Add (room);
				} else {
					roomListA.Add (room);
				}
			}
		} else {
			roomListA = allRooms;
			roomListB = allRooms;
		}

		int bestDistance = 0;
		Coord bestTileA = new Coord ();
		Coord bestTileB = new Coord ();
		Room bestRoomA = new Room ();
		Room bestRoomB = new Room ();
		bool posibleConnectionFound = false;

		foreach (Room roomA in roomListA) {
			if (!forceAccessibilityFromMainRoom) {
				posibleConnectionFound = false;
				if (roomA.connectedRooms.Count > 0) {
					continue;
				}
			}
			foreach (Room roomB in roomListB) {
				if(roomA == roomB || roomA.IsConnected(roomB)){
					continue;
				}
				for(int tileIndexA =0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++){
					for(int tileIndexB =0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++){
						Coord tileA = roomA.edgeTiles [tileIndexA];
						Coord tileB = roomB.edgeTiles [tileIndexB];
						int distanceBetweenRooms = (int) (Math.Pow(tileA.tileX - tileB.tileX, 2) + Math.Pow(tileA.tileY - tileB.tileY, 2));

						if(distanceBetweenRooms < bestDistance || !posibleConnectionFound){
							bestDistance = distanceBetweenRooms;
							posibleConnectionFound = true;
							bestTileA = tileA;
							bestTileB = tileB;
							bestRoomA = roomA;
							bestRoomB = roomB;
						}
					}
				}
			}
			if (posibleConnectionFound && !forceAccessibilityFromMainRoom) {
				CreatePassage (bestRoomA, bestRoomB, bestTileA, bestTileB);
			}
		}

		if (posibleConnectionFound && forceAccessibilityFromMainRoom) {
			CreatePassage (bestRoomA, bestRoomB, bestTileA, bestTileB);
			ConnectClosestRooms (allRooms, true);
		}

		if(!forceAccessibilityFromMainRoom){
			ConnectClosestRooms (allRooms, true);
		}
	}


	void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB){
		Room.ConnectRooms (roomA, roomB);
		Debug.DrawLine (CoordToWorldPoint (tileA), CoordToWorldPoint (tileB), Color.green, 100);

		List<Coord> line = GetLine (tileA, tileB);
		foreach(Coord c in line){
			DrawCircle (c, 1);
		}

	}

	void DrawCircle(Coord c, int r){
		for(int x = -r;x<= r;x++){
			for(int y = -r;y<= r;y++){
				if (x * x + y * y <= r * r) {
					int realX = c.tileX + x;
					int realY = c.tileY + y;
					if(IsInMapRange(realX, realY)){
						map [realX, realY] = 0;
					}
				}	
			}
		}
	}

	List<Coord> GetLine(Coord from, Coord to){
		List<Coord> line = new List<Coord> ();
		int x = from.tileX, y = from.tileY;
		int dx = to.tileX - from.tileX;
		int dy = to.tileY - from.tileY;
		bool inverted = false;
		int step = Math.Sign (dx);
		int gradientStep = Math.Sign (dy);

		int longest = Mathf.Abs (dx);
		int shortest = Mathf.Abs (dy);

		if (longest < shortest) {
			inverted = true;
			longest = Mathf.Abs (dy);
			shortest = Mathf.Abs(dx);

			step = Math.Sign (dy);
			gradientStep = Math.Sign (dx);


		}

		int gradientAccumulation = longest / 2;
		for(int i =0; i< longest;i++){
			line.Add (new Coord(x,y));
			if (inverted) {
				y += step;
			} else {
				x += step;
			}
			gradientAccumulation += shortest;
			if(gradientAccumulation >= longest){
				if (inverted) {
					x += gradientStep;
				} else {
					y += gradientStep;
				}
				gradientAccumulation -= longest;
			}
		}
		return line;
	}

	Vector3 CoordToWorldPoint(Coord tile){
		return new Vector3 (-width/2+.5f+tile.tileX, 2, -height/2 + .5f+tile.tileY);
	}
	/*
		Obtiene regiones del mapa que componen un espacio
		1 pared
		0 vacio
	*/
	List<List<Coord>> GetRegions(int tileType){ 
		List<List<Coord>> regions = new List<List<Coord>> ();
		int[,] mapFlags = new int[width, height];

		for(int x=0;x<width;x++){
			for(int y=0;y<height;y++){
				if(mapFlags[x,y] == 0 && map[x,y] == tileType){
					List<Coord> newRegion = GetRegionTiles(x,y);
					regions.Add(newRegion);
					foreach(Coord tile in newRegion){
						mapFlags[tile.tileX, tile.tileY] = 1;
					}
				}
			}
		}
		return regions;
	}

	void RandomFillMap(){		//Funcion para llenar el mapa 
		if (useRandomSeed) {
			seed = Time.time.ToString ();
		}
		System.Random pseudoRandom = new System.Random (seed.GetHashCode ());
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				if (x == 0 || y == 0 || x == width - 1 || y == height - 1) {	//Se considera la orilla 
					map [x, y] = 1;
				} else {
					map [x, y] = (pseudoRandom.Next (0, 100) < randomFillPercent)? 1:0 ;
				}

			}
		}
	}

	List<Coord> GetRegionTiles(int startX, int startY){
		List<Coord> tiles = new List<Coord> ();
		int[,] mapFlags = new int[width, height];
		int tileType = map [startX, startY];

		Queue<Coord> queue = new Queue<Coord> ();
		queue.Enqueue (new Coord(startX, startY));
		mapFlags [startX, startY] = 1;

		while(queue.Count > 0){
			Coord tile = queue.Dequeue ();
			tiles.Add (tile);

			for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++) {
				for (int y = tile.tileY - 1 ; y <= tile.tileY +  1; y++) {
					if(IsInMapRange(x, y) && (y == tile.tileY || x == tile.tileX)){
						if(mapFlags[x,y] == 0 && map[x,y] == tileType){
							mapFlags [x, y] = 1;
							queue.Enqueue (new Coord(x,y));
						}
					}
				}
			}
		}
		return tiles;
	}

	bool IsInMapRange(int x, int y){
		return x >= 0 && x < width && y >= 0 && y < height;
	}

	void SmoothMap(){		//Se encarga de unir paredes y espacios vacios
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				int neighbourWallTiles = GetSurrondingWallCount (x,y);
				if (neighbourWallTiles > 4)
					map [x, y] = 1;
				else if (neighbourWallTiles < 4)
					map [x, y] = 0;
			}
		}
	}

	int GetSurrondingWallCount(int gridX, int gridY){
		int wallCount = 0;
		for(int neighbourX = gridX -1; neighbourX <= gridX +1; neighbourX ++ ){
			for(int neighbourY = gridY -1; neighbourY <= gridY +1; neighbourY ++ ){
				if(IsInMapRange(neighbourX, neighbourY)){
					if(neighbourX != gridX || neighbourY != gridY)
					{
						wallCount += map [neighbourX, neighbourY];
					}
				}else{
					wallCount++;
				}
			}
		}
		return wallCount;
	}

	struct Coord{
		public int tileX;
		public int tileY;

		public Coord(int x, int y){
			tileX = x;
			tileY = y;
		}
	}
	class Room : IComparable<Room>{
		public List<Coord> tiles;
		public List<Coord> edgeTiles;
		public List<Room> connectedRooms;
		public int roomSize;
		public bool isAccessibleFromMainRoom;
		public bool isMainRoom;

		public Room(){
			
		}

		public Room(List<Coord> roomTiles, int[,] map){
			tiles = roomTiles;
			roomSize = tiles.Count;
			connectedRooms = new List<Room>();
			edgeTiles = new List<Coord>();

			foreach(Coord tile in tiles){
				for(int x = tile.tileX - 1 ; x <= tile.tileX + 1; x++){
					for(int y = tile.tileY - 1 ; y <= tile.tileY + 1; y++){
						if(x == tile.tileX || y == tile.tileY){
							if(map[x,y] == 1){
								edgeTiles.Add(tile);
							}
						}
					}
				}
			}
		}
		public void SetAccessibleFromMainRoom(){
			if (!isAccessibleFromMainRoom) {
				isAccessibleFromMainRoom = true;
				foreach(Room connectedRoom in connectedRooms){
					connectedRoom.SetAccessibleFromMainRoom ();
				}
			}
		}

		public static void ConnectRooms (Room roomA, Room roomB){
			if (roomA.isAccessibleFromMainRoom) {
				roomB.SetAccessibleFromMainRoom ();
			} else {
				if(roomB.isAccessibleFromMainRoom){
					roomA.SetAccessibleFromMainRoom ();
				}
			}
			roomA.connectedRooms.Add (roomB);
			roomB.connectedRooms.Add (roomA);
		}

		public bool IsConnected(Room otherRoom){
			return connectedRooms.Contains (otherRoom);
		}
		public int CompareTo(Room otherRoom){
			return otherRoom.roomSize.CompareTo (roomSize);
		}

	}
}
