#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Debug = UnityEngine.Debug;


public class LevelEditor : EditorWindow
{

	/*
	 * Modified version of Map Maker 2D by João Ramiro
	 * Level editor for Project Laser
	 */

	#region variable declaration

	//instance of this script
	public static LevelEditor Instance;

	//scroll position of the editor window
	private Vector2 _scrollPos;

	//bool that lets create in the same layer, an object above the other or not
	//public bool Overwrite;
	//public static bool OverwriteState;

	//lets you place an object not snapped to the grid
	//public bool Snapping;
	//public static bool SnappingState;

	//public bool Flipping;
	//public static bool FlippingState;

    //controls when there will be an area deletion
	public bool AreaDel;

	//control when there will be an area insertion
	public bool AreaIns;

	//used to align prefabs that are not 1x1 to the grid
	private Vector2 _align;
	private int _alignId;

	//hold layer where objects will be placed
	private Layer _currentLayer;


	//holds reference to this window
	private static LevelEditor _window;

	//controls rotation of the object to place
	public int RotationMode;
	public static bool RotationState;
	public static int RotationModeStatic;

	//hold spriteRenderer of the GizmoTile
	private static SpriteRenderer _gizmoTileSR;


	//tells us if mapTool is currently being used or not
	private bool _mapToolActive = true;


	//first and final positions while dragging
	public Vector3 BeginPos;
	public Vector3 EndPos;

	//holds if shift was pressed and since then there wasn't a mouse up event
	public bool ShiftPressed;

	//last tool (Move/rotate/scale/rect) used before using mapTool
	public Tool LastTool;

	//hold if mouse is currently down
	public static bool MouseDown;

	//gizmos that appear on the scene to help build the map
	public static GameObject GizmoCursor, GizmoTile;
	public static Sprite FullCursor;

	//mousePosition at all times
	public Vector2 MousePos;

	//true if you want to debug asset
	public bool ShowConsole = true;

	//holds all prefabs that can be placed on the scene
	private static List<GameObject> _allPrefabs;

	//holds prefabs with category
	private static Dictionary<string, List<GameObject>> _prefabCategory;

    //save category UI open status
	private Dictionary<string, bool> _categoryOpen = new Dictionary<string, bool>();

    //holds all the existing layers
	public List<Layer> LayerList = new List<Layer>();
    public Floor FloorLayer;

	//used to make the whole tool work in some magic way
	public int ControlID;

	//item selected on the prefab selection grid
	public int PrevSelGridInt = 0;
	public int SelGridInt = 0;
	public static int SelGridIntStatic = 0;
	public static bool SelGridIntState;

    //Aligner GUI
	private bool _showAlign = false;

	//CurrentTile
	public GameObject CurPrefab;

	//reorderable list that can be seen on the editor window
	public ReorderableList LayerReorder;
    

	#endregion variable

	#region Tool Initialization

	// Quickly opens app with Ctrl+M
	[MenuItem("Level Editor/Open %m", false, 1)]
	static void Init()
	{
		// Get existing open window or if none, make a new one:
		_window = (LevelEditor)GetWindow(typeof(LevelEditor));
		_window.Show();

		//sets minimum size
		_window.minSize = new Vector2(200, 315);

		//sets window title
		_window.titleContent = new GUIContent("Level Editor");
	}

	//runs whenever the window is created
	private void OnEnable()
	{

		ShowLog("Enabled");

        FullCursor = Resources.Load<Sprite>("Sprite/Cursor");
        //QuarterCursor = Resources.Load<Sprite>("Sprite/QuarterCursor");

        //sets alignment to middle center
		_alignId = 4;
		_align = Vector2.zero;

		//sets selected prefab to the first one
		SelGridInt = 0;

		//sets instance of mapmaker
		Instance = this;

		//sets rotation to zero , overwrite on, snapping on
		RotationMode = 0;
		//Overwrite = true;
		//Snapping = true;

		//sets up the reorderable list

		//makes it reorder elements on layer list
		LayerReorder = new ReorderableList(LayerList, typeof(Layer), true, true, true, true);

		//sets delegate to run when + button is pressed
		LayerReorder.onAddCallback += AddLayer;
		//sets delegate to run when - button is pressed
		LayerReorder.onRemoveCallback += RemoveLayer;
		//writes wanted title
		LayerReorder.drawHeaderCallback += DrawHeader;
		//writes elements of list in the desired format
		LayerReorder.drawElementCallback += DrawElement;
		//organizes layers when list is reordered
		LayerReorder.onReorderCallback += OrganizeLayers;

		LayerReorder.onMouseUpCallback += SelectLayer;

		//initializes drag positions
		BeginPos = Vector3.zero;
		EndPos = Vector3.zero;

		//loads layers currently on the scene
		LoadLayers();

		//if there are no layers, create one and set is to be used
		if (LayerList.Count == 0)
		{
			AddLayer(LayerReorder);
			_currentLayer = LayerList[0];
		}

        if (FloorLayer == null)
        {
            //creates floor
            ShowLog("Floor Creation");
            GameObject floorObj = new GameObject("Floor");
            Undo.RegisterCreatedObjectUndo(floorObj, "Created layer");
            floorObj.AddComponent<Floor>();
            Floor floor = floorObj.GetComponent<Floor>();
            FloorLayer = floor;
        }

		//makes some code run onSceneGUI
		SceneView.duringSceneGui += SceneGUI;
	}

	#endregion

	//Loads All prefabs
	private void LoadPrefabs()
	{
		//if there are no prefabs sets up list
		if (_allPrefabs == null)
			_allPrefabs = new List<GameObject>();
		if (_prefabCategory == null)
			_prefabCategory = new Dictionary<string, List<GameObject>>();

		//clears list
		_allPrefabs.Clear();

		//Get categories
		string[] categories = AssetDatabase.GetSubFolders("Assets/Resources/Prefab");
		for (var i = 0; i < categories.Length; i++)
		{
			categories[i] = categories[i].Replace("Assets/Resources/Prefab/", "");
		}

		//Sort prefabs by category
		foreach (var category in categories)
		{
			//adds to the list all prefabs on the resources folder
			var loadedObjects = Resources.LoadAll("Prefab/" + category);
			if (loadedObjects.Length > 0)
			{
				if (_prefabCategory.ContainsKey(category))
				{
					_prefabCategory[category] = new List<GameObject>();
				}
				else _prefabCategory.Add(category, new List<GameObject>());
				if (!_categoryOpen.ContainsKey(category)) _categoryOpen.Add(category, true);

				foreach (var loadedObject in loadedObjects)
				{
					if (loadedObject is GameObject gameObject && gameObject.name != "TilePointerGizmo")
                    {
                        var option = gameObject.GetComponent<PrefabOption>();
                        if (!option || (option && option.showInEditor))
                        {
                            _prefabCategory[category].Add(gameObject);
                            _allPrefabs.Add(gameObject);
						}
                    }
				}
			}
		}
	}
	private bool IsSimilarVec(Vector2 lhs, Vector2 rhs)
    {
        float diff = (lhs - rhs).magnitude;
        return diff < 0.01f;
    }
	//Finds object in certain position
	//returns null if not found
	private GameObject IsObjectAt(Vector2 tilePos, Transform layerTransform)
    {
		/*
        var qBool = Physics2D.queriesStartInColliders;
		Physics2D.queriesStartInColliders = true;
        var rays = Physics2D.RaycastAll(tilePos, Vector2.zero);
        Physics2D.queriesStartInColliders = qBool;
        foreach (var ray in rays)
        {
            if (ray.transform && ray.transform.parent
                              && ray.transform.parent.gameObject.Equals(curLayer.gameObject))
            {
                GameObject g = ray.transform.gameObject;
                if (g.Equals(GizmoTile)) continue;
                ArtificialPosition artPos = g.GetComponent<ArtificialPosition>();
                if (artPos == null)
                {
                    if (ray.transform.parent.parent == null)
                    {
                        return g;
                    }
                }
                else if (IsSimilarVec(artPos.position, tilePos) && g.name != "GizmoCursor")
                {
                    return g;
                }
            }
		}
        */
        
		//looks for the object on the current active layer
		for (int i = 0; i < layerTransform.childCount; i++)
		{
			GameObject g = layerTransform.GetChild(i).gameObject;

			ArtificialPosition artPos = g.GetComponent<ArtificialPosition>();

			//its a normal item
			if (artPos == null)
			{

				//finds it
				if (IsSimilarVec(g.transform.localPosition, tilePos) && (g.name != "GizmoCursor" && g.name != "GizmoTile"))
				{

					//for safety (in case there are child child prefabs)
					if (g.transform.parent.parent == null)
					{
						return g;
					}
				}
			}
			else
			{
				//handle non 1x1 game objects
				if (IsSimilarVec(artPos.position, tilePos) && g.name != "GizmoCursor")
				{
					return g;
				}
			}
		}
		
        return null;
	}

	//whenever window is closed
	private void OnDisable()
	{
		//sets last tool
		Tools.current = LastTool;

		//deletes auxiliary gizmos
		DestroyImmediate(GameObject.Find("GizmoTile"));
		DestroyImmediate(GameObject.Find("GizmoCursor"));

		//clears delegates // Make sure we don't get memory leaks etc.

		SceneView.duringSceneGui -= SceneGUI;
		LayerReorder.drawHeaderCallback -= DrawHeader;
		LayerReorder.drawElementCallback -= DrawElement;
		LayerReorder.onAddCallback -= AddLayer;
		LayerReorder.onRemoveCallback -= RemoveLayer;
		LayerReorder.onMouseUpCallback -= SelectLayer;

	}

	//check if we are not 
	private void OnLostFocus()
	{
        PrevSelGridInt = SelGridInt;
	}


	//Happens every time the window is focused (clicked)
	private void OnFocus()
	{

		//activates tool
		if (_mapToolActive)
			ActivateMapTool();

		//loads prefabs again
		LoadPrefabs();

		//finds sprite renderer of GizmoTile
		if (_gizmoTileSR == null && GizmoTile != null)
			_gizmoTileSR = GizmoTile.GetComponent<SpriteRenderer>();

		_prevAddTilePos = Vector2.positiveInfinity;
	}

	private void DeactivateMapTool()
	{
		PrevSelGridInt = SelGridInt;
		SelGridInt = -1;
		//if a tool was not selected, it fetches last tool used
		if (Tools.current == Tool.None) Tools.current = LastTool;

		//disables gizmos
		if (GizmoTile != null)
			GizmoTile.SetActive(false);
		if (GizmoCursor != null)
			GizmoCursor.SetActive(false);

		//sets mapTool to inactive
		_mapToolActive = false;
	}

	private void ActivateMapTool()
	{
		SelGridInt = PrevSelGridInt;
		//saves tool being used
		LastTool = Tools.current;

		//sets current tool to none
		Tools.current = Tool.None;

		//activate gizmos
		if (GizmoTile != null) GizmoTile.SetActive(true);
		if (GizmoCursor != null) GizmoCursor.SetActive(true);
		ChangeGizmoTile();
		//sets mapTool to active
		_mapToolActive = true;

	}


	//Updates with the scene
	private void SceneGUI(SceneView sceneView)
	{

		//fetches current
		Event e = Event.current;

		//Reactivate tool by pressing M only works when scene view is focused
		if (e.type == EventType.KeyDown && e.keyCode == KeyCode.M)
		{

			ActivateMapTool();
		};

		//if right click toggle mapTool ON/OFF
		if (e.type == EventType.MouseUp && e.button == 1)
		{

			//toggle tool on or off
			_mapToolActive = !_mapToolActive;

			if (_mapToolActive == false)
			{
				DeactivateMapTool();

			}
			else
				ActivateMapTool();

		}

		//on ESC pressed toggle mapTool ON/OFF
		if (e.type == EventType.KeyUp && e.keyCode == KeyCode.Escape)
		{

			//toggle tool on or off
			_mapToolActive = !_mapToolActive;

			if (_mapToolActive == false)
			{
				DeactivateMapTool();

			}
			else
				ActivateMapTool();

		}

		//getsMousePosition
		MousePos = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;


		//if user wants to used other tool (rect, scale etc ) (user pressed keys shortcuts for it (e,r,t) or clicked btn) 
		if (_mapToolActive == true && Tools.current != Tool.None)
		{

			DeactivateMapTool();
		}

		//stops tool from working this point onward
		if (_mapToolActive == false)
		{
			return;
		}

		//Weird stuff that editorwindows with controlls have to have
		//Sets ControlID
		ControlID = GUIUtility.GetControlID(FocusType.Passive);

		if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(ControlID);

		switch (e.type)
		{
			//checks if left click is being pressed
			case EventType.MouseDown:
				{
					if (e.button == 0) //LEFT CLICK DOWN
					{
						MouseDown = true;
					}
					break;
				}
			case EventType.MouseUp:
				{
					if (e.button == 0) //LEFT CLICK UP
					{
						MouseDown = false;
					}
					break;
				}
			case EventType.KeyDown:
				{
					//activates tool by pressing m
					if (e.keyCode == KeyCode.M)
					{

						ActivateMapTool();
					}
				}
				break;
		}


		//sets shift was pressed if shift was being pressed while mouse was down
		if (e.shift && MouseDown)
		{

			ShiftPressed = true;
		}
		//Add Single tile on mouse down
		if (MouseDown && e.shift == false && AreaIns == false && e.control == false)
		{
            //prevents from creating several gameobjects if snapping is off
			//if (Snapping == false)
			//	MouseDown = false;
            var layerTransform = _currentLayer.transform;
            if (CheckOnlyOnFloor(CurPrefab))
                layerTransform = FloorLayer.transform;
			//adds a tile
			AddTile(GizmoCursor.transform.position, layerTransform);
		}
		//Add Multiple tiles
		if (MouseDown && ShiftPressed && AreaIns == false && e.control == false)
		{

			AreaIns = true;

			//if was not on AreaDel set new BeginPos
			if (AreaDel == false) BeginPos = GizmoCursor.transform.position;

			AreaDel = false;

			ShowLog("Started Area Insertion");
		}

		//Starts AreaDeletion
		if (MouseDown && ShiftPressed && AreaDel == false && e.control == true)
		{
			AreaDel = true;

			//if was not on areainsertion set new beginposition
			if (AreaIns == false) BeginPos = GizmoTile.transform.position;

			AreaIns = false;
			ShowLog("Started Area Deletion");
		}


		//Draws Rectangle and repaints scene view
		if (AreaIns || AreaDel)
		{

			DrawAreaRectangle();
			SceneView.RepaintAll();
		}

		//Deletes Elements in that area
		if (MouseDown == false && AreaDel == true && ShiftPressed && e.control)
		{

			AreaDeletion();
			AreaDel = false;
		}

		//Instantiates elements in that area
		if (MouseDown == false && AreaIns == true && ShiftPressed && e.control == false)
		{
			AreaInsertion();
			AreaIns = false;
		}


		//Removes single tile by Ctrl+Click
		if (MouseDown && e.control && AreaDel == false)
		{
			Debug.Log("try remove");
			RemoveTile();
		}

		//if mouse is up, shift was not pressed
		if (MouseDown == false)
			ShiftPressed = false;


		//updates cursor position
		CursorUpdate();

		//repaints editorwindow
		Repaint();

	}
	/*
	//Rotates GizmoTile -90degrees
	[MenuItem("Level Editor/Rotate CW &r", false, 12)]
	private static void RotateGizmo()
	{
		if (Instance == null) return;
		if (!CheckRotatable(Instance.CurPrefab)) return;
		RotationState = true;
		RotationModeStatic = (RotationModeStatic + 3) % 4;
		Undo.RecordObject(Instance, "Rotate CW");
		//GizmoTile.transform.rotation = Quaternion.Euler(0,0,rotation);

	}

	//Rotates GizmoTile +90degrees
	[MenuItem("Level Editor/Rotate CCW #&r", false, 12)]
	private static void RotateCounterGizmo()
	{
		if (Instance == null) return;
		if (!CheckRotatable(Instance.CurPrefab)) return;
		RotationState = true;
		RotationModeStatic = (RotationModeStatic + 1) % 4;
		Undo.RecordObject(Instance, "Rotate CCW");
		//Undo.RecordObject (GizmoTile.transform, "Rotation");
		//GizmoTile.transform.rotation = Quaternion.Euler(0,0,rotation);

	}
	*/
	/*
	//toggles snapping
	[MenuItem("Level Editor/Snap &s", false, 24)]
	private static void ToggleSnapping()
	{
		if (Instance == null) return;

		Undo.RegisterFullObjectHierarchyUndo(Instance, "Snapping Shortcut");
		SnappingState = true;
		EditorUtility.SetDirty(Instance);
	}


	//toggles Y flipping
	[MenuItem("Level Editor/Flip &d", false, 24)]
	private static void FlipSprite()
	{
		if (Instance == null) return;
		if (!CheckFlippable(Instance.CurPrefab)) return;
		FlippingState = true;
		Undo.RecordObject(Instance, "Flipping");
	}

	//toggles overwriting
	[MenuItem("Level Editor/OverWrite &a", false, 24)]
	private static void ToggleOverWrite()
	{
		if (Instance == null) return;

		OverwriteState = true;
		Undo.RecordObject(Instance, "Overwrite");
	}
	*/
	//Draws Rectangle Area
	private void DrawAreaRectangle()
	{
		//Gets bounding box
		Vector4 area = GetAreaBounds();

		//DrawsLines
		//top line
		Handles.DrawLine(new Vector3(area[3] + 0.5f, area[0] + 0.5f, 0), new Vector3(area[1] - 0.5f, area[0] + 0.5f, 0));
		//down line
		Handles.DrawLine(new Vector3(area[3] + 0.5f, area[2] - 0.5f, 0), new Vector3(area[1] - 0.5f, area[2] - 0.5f, 0));
		//left line
		Handles.DrawLine(new Vector3(area[3] + 0.5f, area[0] + 0.5f, 0), new Vector3(area[3] + 0.5f, area[2] - 0.5f, 0));
		//right line
		Handles.DrawLine(new Vector3(area[1] - 0.5f, area[0] + 0.5f, 0), new Vector3(area[1] - 0.5f, area[2] - 0.5f, 0));
	}

	//Corrects area bounds
	private Vector4 GetAreaBounds()
	{
		Vector2 topLeft;
		Vector2 downRight;

		//sets endposition for drag
		EndPos = GizmoCursor.transform.position;

		//finds vertices
		topLeft.y = EndPos.y > BeginPos.y ? EndPos.y : BeginPos.y;
		topLeft.x = EndPos.x < BeginPos.x ? BeginPos.x : EndPos.x;
		downRight.y = EndPos.y > BeginPos.y ? BeginPos.y : EndPos.y;
		downRight.x = EndPos.x < BeginPos.x ? EndPos.x : BeginPos.x;

		return new Vector4(topLeft.y, downRight.x, downRight.y, topLeft.x);
	}

	//SHOULD BE LOOKED AT AGAIN
	private Vector3 OffsetWeirdTiles()
	{
		//TODO ONLY WORKS FOR ONE BIG OBJECT, instead of parent of several objects
		if (_gizmoTileSR != null && _gizmoTileSR.sprite != null && (_gizmoTileSR.sprite.bounds.extents.x != 0.5f || _gizmoTileSR.sprite.bounds.extents.y != 0.5f))
			//the -0.5f is to center it correctly
			return new Vector3(-_align.x * (_gizmoTileSR.sprite.bounds.extents.x - 0.5f), _align.y * (_gizmoTileSR.sprite.bounds.extents.y - 0.5f), 0);

		return Vector3.zero;
	}

	//Delete gameObjects in an area
	private void AreaDeletion()
	{
		Vector2 topLeft;
		Vector2 downRight;

		EndPos = GizmoTile.transform.position;

		//fetches vertices
		topLeft.y = EndPos.y > BeginPos.y ? EndPos.y : BeginPos.y;
		topLeft.x = EndPos.x < BeginPos.x ? BeginPos.x : EndPos.x;
		downRight.y = EndPos.y > BeginPos.y ? BeginPos.y : EndPos.y;
		downRight.x = EndPos.x < BeginPos.x ? EndPos.x : BeginPos.x;


        var layerTransform = _currentLayer.transform;
        if (CheckOnlyOnFloor(CurPrefab))
        {
            layerTransform = FloorLayer.transform;
        }

		//Goes through all units and deletes the objects if there is one
		for (var y = downRight.y; y <= topLeft.y; y++)
		{

			for (var x = downRight.x; x <= topLeft.x; x++)
			{
				var pos = new Vector2(x,y);
                var objToDelete = IsObjectAt(pos, layerTransform);
				//If there's something then delete it
				if (objToDelete == null) continue;
				Undo.DestroyObjectImmediate(objToDelete);
				//DestroyImmediate(goToDelete);
			}
		}

	}

	//Inserts area of gameObject
	private void AreaInsertion()
	{
		Vector2 topLeft;
		Vector2 downRight;

		EndPos = GizmoTile.transform.position;

		topLeft.y = EndPos.y > BeginPos.y ? EndPos.y : BeginPos.y;
		topLeft.x = EndPos.x < BeginPos.x ? BeginPos.x : EndPos.x;
		downRight.y = EndPos.y > BeginPos.y ? BeginPos.y : EndPos.y;
		downRight.x = EndPos.x < BeginPos.x ? EndPos.x : BeginPos.x;

        bool OnEdge(float x, float y)
        {
            return Math.Abs(x - downRight.x) < 0.001f 
                   || Math.Abs(x - topLeft.x) < 0.001f
                   || Math.Abs(y - downRight.y) < 0.001f 
                   || Math.Abs(y - topLeft.y) < 0.001f;
        }

        var layerTransform = _currentLayer.transform;
        if (CheckOnlyOnFloor(CurPrefab))
        {
            layerTransform = FloorLayer.transform;
        }

        var onFloor = layerTransform == FloorLayer.transform;
		//goes through every unit on that area and creates objects
		for (float y = downRight.y; y <= topLeft.y; y++)
		{
			for (float x = downRight.x; x <= topLeft.x; x++)
            {
                GameObject go = IsObjectAt(new Vector3(x, y, 0), layerTransform);
                if (!onFloor)
                {
                    GameObject floor = IsObjectAt(new Vector3(x, y, 0), FloorLayer.transform);
                    if (floor == null) continue;
                    if (!floor.CompareTag("Floor")) continue;
				}
                //If there no object than create it
				if (go == null)
				{
                    InstantiateTile(new Vector3(x, y, 0), layerTransform);

                }//in this case there is go in there
				else// if (Overwrite)
				{
					Undo.DestroyObjectImmediate(go);
					DestroyImmediate(go);

                    InstantiateTile(new Vector3(x, y, 0), layerTransform);
				}
            }
		}
	}

	//when inspector is updated, reload layer if they are none
	private void OnInspectorUpdate()
	{

	}

	//Updates the gizmos on the screen
	private void CursorUpdate()
	{
		//firs tries to find gizmos if they are null
		if (GizmoCursor == null)
			GizmoCursor = GameObject.Find("GizmoCursor");

		if (GizmoTile == null)
			GizmoTile = GameObject.Find("GizmoTile");

		//in case they are inactive this must be done to find them
		if (GizmoTile == null || GizmoCursor == null)
		{
			Transform[] t = Resources.FindObjectsOfTypeAll<Transform>();
			foreach (var item in t)
			{
				if (item.name == "GizmoCursor")
				{
					GizmoCursor = item.gameObject;
					GizmoCursor.SetActive(true);
				}

				if (item.name == "GizmoTile")
				{
					GizmoTile = item.gameObject;
					GizmoTile.SetActive(true);

				}

			}
		}

		//Creates the if they dont already exist
		if (GizmoCursor == null)
		{

			//creates gizmo cursor
			GameObject pointer = (GameObject)Resources.Load("TilePointerGizmo", typeof(GameObject));
			if (pointer != null) GizmoCursor = (GameObject)Instantiate(pointer);
			else GizmoCursor = new GameObject();


			GizmoCursor.name = "GizmoCursor";
			GizmoCursor.hideFlags = HideFlags.HideInHierarchy;
			ShowLog("Cursor Created");

		}
		if (GizmoTile == null)
		{

			//if there are tiles change gizmo
			if (_allPrefabs != null && _allPrefabs.Count > 0)
				ChangeGizmoTile();
			else
				//creates gizmo cursor with default gizmo
				GizmoTile = new GameObject();

			GizmoTile.hideFlags = HideFlags.HideInHierarchy;
		}


		//position cursor in correct place
		if (GizmoCursor != null)
		{


			//check if snaping is active and position cursor
			//if (Snapping)
			//{
			Vector2 gizmoPos = Vector2.zero;
			if (MousePos.x - Mathf.Floor(MousePos.x) < 0.5f)
			{
				gizmoPos.x = Mathf.Floor(MousePos.x) + 0.5f;
			}
			else if (Mathf.Ceil(MousePos.x) - MousePos.x < 0.5f)
			{
				gizmoPos.x = Mathf.Ceil(MousePos.x) - 0.5f;
			}
			if (MousePos.y - Mathf.Floor(MousePos.y) < 0.5f)
			{
				gizmoPos.y = Mathf.Floor(MousePos.y) + 0.5f;
			}
			else if (Mathf.Ceil(MousePos.y) - MousePos.y < 0.5f)
			{
				gizmoPos.y = Mathf.Ceil(MousePos.y) - 0.5f;
			}

			//GizmoCursor.transform.rotation = Quaternion.Euler(0, 0, ModeToAngle(RotationMode));
            GizmoTile.transform.rotation = Quaternion.Euler(0, 0, ModeToAngle(RotationMode));

			//sets gizmo cursor and tile positions
			GizmoCursor.transform.position = gizmoPos;
			GizmoTile.transform.position = gizmoPos + (Vector2)GizmoTile.transform.InverseTransformVector(OffsetWeirdTiles());
			//}
			/*
			else
			{

				////sets gizmo cursor and tile positions
				GizmoCursor.transform.position = MousePos;
				GizmoTile.transform.position = MousePos;

			}
			*/
			//Scale the scale correctly
			if (CurPrefab != null) GizmoTile.transform.localScale = CurPrefab.transform.localScale;
		}
	}

	private static float ModeToAngle(int mode)
	{
		return mode * 90f;
	}
	//Instantiate one tile
	private GameObject InstantiateTile(Vector2 pos, Transform layerTransform)
	{

		//Only creates tile if mouse is over scene view
		if (CurPrefab == null || mouseOverWindow.ToString() != " (UnityEditor.SceneView)")
			return null;
        //creates objects, rotates it, and parents it to the layer

		GameObject metaTile = (GameObject)PrefabUtility.InstantiatePrefab(CurPrefab);


		metaTile.transform.rotation = Quaternion.Euler(0, 0, ModeToAngle(RotationMode));
		metaTile.transform.SetParent(layerTransform);
		metaTile.transform.localPosition = (Vector3)pos + metaTile.transform.InverseTransformVector(OffsetWeirdTiles());


		//IF it is a weird shape
		if (metaTile.transform.localPosition != (Vector3)pos)
		{
			ArtificialPosition artPos = metaTile.AddComponent<ArtificialPosition>();
			artPos.position = pos;
			artPos.offset = artPos.position - (Vector2)metaTile.transform.position;
			artPos.layer = _currentLayer;
		}

		//gets renderer
		SpriteRenderer sr = metaTile.GetComponent<SpriteRenderer>();

		//sets order in layer
		if (sr != null)
		{
			sr.sortingOrder = LayerList.Count - LayerList.IndexOf(_currentLayer);

			if (_gizmoTileSR != null) sr.flipY = _gizmoTileSR.flipY;
		}

		//var option = metaTile.GetComponent<PrefabOption>();
		//option.OnCreation();

		Undo.RegisterCreatedObjectUndo(metaTile, "CreatedTile");

		return metaTile;
	}

	private Vector2 _prevAddTilePos = Vector2.positiveInfinity;
	private void AddTile(Vector2 pos, Transform layerTransform)
	{
        //only adds tile if mouse is over sceneView
		if (mouseOverWindow.ToString() != " (UnityEditor.SceneView)")
		{
			MouseDown = false;
			return;
		}

        if (_prevAddTilePos == pos) return;

        //sees if there's an object at that position
		GameObject obj = IsObjectAt(pos, layerTransform);
        if (layerTransform != FloorLayer.transform)
        {
            GameObject floor = IsObjectAt(pos, FloorLayer.transform);
            if (floor == null) return;
            if (!floor.CompareTag("Floor")) return;
		}
        

		//creates object/ovewrites current one in that position

		if (obj == null)
		{

			Undo.RegisterFullObjectHierarchyUndo(layerTransform, "Created object");
			InstantiateTile(pos, layerTransform);
		}
		else// if (Overwrite)
		{
            Undo.RegisterFullObjectHierarchyUndo(layerTransform, "Created object");

			Undo.DestroyObjectImmediate(obj);
			DestroyImmediate(obj);
			InstantiateTile(pos, layerTransform);
        }

        _prevAddTilePos = pos;
    }

	//Deletes object at a certain location
	private void RemoveTile()
    {
        var layerTransform = _currentLayer.transform;
        if (CheckOnlyOnFloor(CurPrefab))
        {
            layerTransform = FloorLayer.transform;
        }

        Vector2 pos = GizmoCursor.transform.position;
		GameObject objToDelete = IsObjectAt(pos, layerTransform);
		Debug.Log(objToDelete);
        if (objToDelete)
        {
	        Undo.DestroyObjectImmediate(objToDelete);
            //DestroyImmediate(goToDelete);
        }

        _prevAddTilePos = Vector2.positiveInfinity;
	}

	private void OnDestroy()
	{
		OnDisable();
	}

	//turns into ghost all children of object, by using recurrence
	private static void MakeGhost(GameObject go)
	{
		if (go.GetComponent<SpriteRenderer>() != null)
		{
			Color c = go.GetComponent<SpriteRenderer>().color;
			c.a = 0.5f;
			go.GetComponent<SpriteRenderer>().color = c;
		}

		foreach (Transform t in go.transform)
		{
			MakeGhost(t.gameObject);
		}
	}

	/*
	private static bool CheckRotatable(GameObject obj)
	{
		var prop = obj.GetComponent<PrefabOption>();
		return !prop || prop.rotatable;
	}

	private static bool CheckFlippable(GameObject obj)
	{
		var prop = obj.GetComponent<PrefabOption>();
		return !prop || prop.flippable;
	}
	*/
	private static bool CheckOnlyOnFloor(GameObject obj)
    {
        if (obj == null) return false;
        var prop = obj.GetComponent<PrefabOption>();
        return prop && prop.onlyOnFloorLayer;
    }

	//Draws gui
	void OnGUI()
	{
		//sets button skin
		GUI.skin.button.alignment = TextAnchor.MiddleCenter;
		GUI.skin.button.imagePosition = ImagePosition.ImageAbove;

		EditorGUILayout.BeginVertical();

		//lets view scroll
		_scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, false, false);

		//label to select prefab
		EditorGUILayout.LabelField("Categories");
		EditorGUI.indentLevel++;
		//If prefabs have been loaded
		//if (_allPrefabs != null && _allPrefabs.Count > 0)
		var startIndex = 0;
		if (_prefabCategory != null && _prefabCategory.Count > 0)
		{

			foreach (var category in _prefabCategory.Keys)
			{
				var prefabs = _prefabCategory[category];
				if (prefabs == null && prefabs.Count <= 0) continue;

				_categoryOpen[category] = EditorGUILayout.Foldout(_categoryOpen[category],
					new GUIContent(category), true);

				//if (EditorGUI.EndChangeCheck()){}
				//if foldout is open
				if (_categoryOpen[category])
				{
					var content = new GUIContent[prefabs.Count];

					//fill it with prefabs, name and image
					for (var i = 0; i < prefabs.Count; i++)
					{
						if (prefabs[i] != null && prefabs[i].name != "")
							content[i] = new GUIContent(prefabs[i].name, AssetPreview.GetAssetPreview(prefabs[i]));


						if (content[i] == null)
							content[i] = GUIContent.none;
					}

					EditorGUI.BeginChangeCheck();

					//prevents from error if objects are deleted by user
					while (SelGridInt >= _allPrefabs.Count)
						SelGridInt--;

					//creates selection grid
					var selected = -1;
					if (SelGridInt >= startIndex && SelGridInt < startIndex + prefabs.Count)
						selected = SelGridInt - startIndex;
					GUILayout.BeginHorizontal();
					GUILayout.Space(EditorGUI.indentLevel * 20);
					var aSelGridInt = GUILayout.SelectionGrid(selected, content,
						5, GUILayout.Height(50 * (Mathf.Ceil(prefabs.Count / (float)5))),
						GUILayout.Width(this.position.width - 30));
					GUILayout.EndHorizontal();
					if (EditorGUI.EndChangeCheck())
					{
						Undo.RegisterCompleteObjectUndo(this, "GUI changed");
						//Undo.RegisterCompleteObjectUndo(_gizmoTileSR,"sr changed")

						if (aSelGridInt != -1)
						{
							if (SelGridInt != aSelGridInt + startIndex)
							{
								SelGridInt = aSelGridInt + startIndex;
								if (!_mapToolActive)
								{
                                    PrevSelGridInt = SelGridInt;
									_mapToolActive = true;
									ActivateMapTool();
								}
							}
							else
							{
                                _mapToolActive = false;
								DeactivateMapTool();
							}
							ChangeGizmoTile();
						}
					}
				}

				startIndex += prefabs.Count;
			}

            CurPrefab = SelGridInt >= 0 ? _allPrefabs[SelGridInt] : null;

			if (SelGridIntState)
			{
				Undo.RegisterCompleteObjectUndo(this, "GUI changed");
				SelGridInt = SelGridIntStatic;
				SelGridIntState = false;
				ChangeGizmoTile();
			}
			else
			{
				SelGridIntStatic = SelGridInt;
				if (GizmoTile != null)
					GizmoTile.transform.rotation = Quaternion.Euler(0, 0, ModeToAngle(RotationMode));
			}
		}
		EditorGUI.indentLevel--;
		EditorGUILayout.Space();


        //control rotation
		EditorGUI.BeginChangeCheck();

        /*
        bool[] rMode = { false, false, false, false };
        rMode[RotationMode] = true;
        using (new EditorGUILayout.HorizontalScope())
        {
	        EditorGUILayout.PrefixLabel(new GUIContent("Rotation", "Rotation angle of tile"));
	        rMode[0] = GUILayout.Toggle(rMode[0], "0", EditorStyles.miniButtonLeft);
	        using (new EditorGUI.DisabledGroupScope(!CurPrefab || !CheckRotatable(CurPrefab)))
	        {
		        rMode[1] = GUILayout.Toggle(rMode[1], "90", EditorStyles.miniButtonMid);
		        rMode[2] = GUILayout.Toggle(rMode[2], "180", EditorStyles.miniButtonMid);
		        rMode[3] = GUILayout.Toggle(rMode[3], "270", EditorStyles.miniButtonRight);
	        }
        }
        */
		//controls flipping
		//var aFlipping = false;
		//using (new EditorGUI.DisabledGroupScope(!CurPrefab || !CheckFlippable(CurPrefab)))
		//	aFlipping = EditorGUILayout.Toggle(new GUIContent("Flipping", "Should tiles flipped"), Flipping);
		//controls overwrite
		//var aOverwrite = EditorGUILayout.Toggle(new GUIContent("Overwrite", "Do you want to overwrite tile in the same layer and position"), Overwrite);
		//controls snapping
		//var aSnapping = EditorGUILayout.Toggle(new GUIContent("Snapping", "Should tiles snap to the grid"), Snapping);

		if (EditorGUI.EndChangeCheck())
		{
			Undo.RegisterCompleteObjectUndo(this, "GUI changed");

			/*
			for (var i = 0; i < 4; i++)
			{
				if (i == RotationMode || !rMode[i]) continue;
				RotationMode = i;
				break;
			}
			*/
			// Code to execute if GUI.changed
			//Snapping = aSnapping;
			//Overwrite = aOverwrite;
			//Flipping = aFlipping;

			//if (_gizmoTileSR != null)
			//	_gizmoTileSR.flipY = Flipping;


			GizmoTile.transform.rotation = Quaternion.Euler(0, 0, ModeToAngle(RotationMode));

			ChangeGizmoTile();
		}

		/*
		if (SnappingState == true)
		{

			Snapping = !Snapping;
			SnappingState = false;
		}
		if (OverwriteState == true)
		{

			Overwrite = !Overwrite;
			OverwriteState = false;
		}
		if (FlippingState == true)
		{

			Flipping = !Flipping;
			FlippingState = false;

			if (_gizmoTileSR != null)
				_gizmoTileSR.flipY = Flipping;

		}
		*/
		if (RotationState)
		{
			RotationMode = RotationModeStatic;
			RotationState = false;
			if (GizmoTile != null) GizmoTile.transform.rotation = Quaternion.Euler(0, 0, ModeToAngle(RotationMode));
		}
		else
		{
			RotationModeStatic = RotationMode;
		}


		EditorGUILayout.Space();


		EditorGUI.BeginChangeCheck();

		//shows alignement foldout
		_showAlign = EditorGUILayout.Foldout(_showAlign, new GUIContent("Alignment", "Used to align objects which size is not 1x1"), true);
		if (EditorGUI.EndChangeCheck())
		{
		}

		//if foldout is open
		if (_showAlign)
		{

			EditorGUI.BeginChangeCheck();

			//draw 3x3 grid with empty string
			_alignId = GUILayout.SelectionGrid(_alignId, new string[9], 3, GUILayout.MaxHeight(40), GUILayout.MaxWidth(40));

			if (EditorGUI.EndChangeCheck())
			{

				//gets new alignement
				_align = AlignId2Vec(_alignId);
			}
		}

		//cleans up LayerList
		for (int i = 0; i < LayerList.Count; i++)
		{
			if (LayerList[i] == null)
				LayerList.Remove(LayerList[i]);
		}

		//prevent error if where is no current layer
		if (_currentLayer == null && LayerList.Count > 0)
		{
			_currentLayer = LayerList[0];
			LayerReorder.index = 0;
		}

		//draws reorderable list
		LayerReorder.DoLayoutList();


		//shows current layer
        if (_currentLayer != null)
        {
            if (CheckOnlyOnFloor(CurPrefab))
            {
                EditorGUILayout.LabelField("Current Layer", "Floor (Special Layer)");
			}
            else
            {
                EditorGUILayout.LabelField("Current Layer", _currentLayer.name);
			}
        }
			


		//Shows the prefab
		EditorGUI.BeginChangeCheck();
		CurPrefab = (GameObject)EditorGUILayout.ObjectField("Current Prefab", CurPrefab, typeof(GameObject), false);

		if (EditorGUI.EndChangeCheck())
		{
			// Code to execute if GUI.changed


			if (_allPrefabs != null)
			{
				//finds prefabs in the list
				int activePre = _allPrefabs.IndexOf(CurPrefab);


				if (activePre > 0)
				{
					SelGridInt = activePre;
					Debug.Log("JUST DO IT");


				}
				//if its not on the list, then addit to it
			}
			else
			{
				//TODO ADD IF NOT ON RESOURCES
				//_allPrefabs.Add (CurPrefab);
				//SelGridInt = _allPrefabs.Count - 1;

			}
		}

		//Displays current prefab
		if (CurPrefab)
        {
            Texture2D previewImage = AssetPreview.GetAssetPreview(GizmoTile);
            GUILayout.Box(previewImage);
		}


        EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();

		Repaint();
	}

	//Debug function
	void ShowLog(object msg)
	{
		if (ShowConsole)
		{
			Debug.Log(msg);
		}
	}

	//whenever hierarchy changes
	void OnHierarchyChange()
	{
		LoadLayers();
		for (int i = 0; i < LayerList.Count; i++)
		{

			//sets priority correctly
			LayerList[i].priority = LayerList.Count - i;

			LayerList[i].transform.SetSiblingIndex(i);

			//sets order in layer correctly
			/*for (int k = 0; k < LayerList[i].transform.childCount; k++) {

				//ShowLog ("List was reordered");
				//Undo.SetTransformParent(LayerList[i].transform,null,"Moved Layers");
				SpriteRenderer sr =	LayerList [i].transform.GetChild (k).GetComponent<SpriteRenderer> ();

				if(sr!=null)
					sr.sortingOrder=LayerList.Count - i;
			}*/
		}
		if (Selection.activeGameObject != null)
		{
			Layer l = Selection.activeGameObject.GetComponent<Layer>();

			if (l != null && _currentLayer != l)
			{
				_currentLayer = l;
				LayerReorder.index = LayerList.IndexOf(l);
			}
		}
	}

	//gets correct alignment
	private Vector2 AlignId2Vec(int alignIndex)
	{
		Vector2 aux;
		aux.x = alignIndex % 3 - 1;
		aux.y = alignIndex / 3 - 1;
		ShowLog(aux);
		return aux;
	}

	//changes gizmo tile sprite
	private void ChangeGizmoTile()
	{
        if (GizmoTile != null)
		{
			Undo.DestroyObjectImmediate(GizmoTile);
		}

		if (SelGridInt < 0) return;
		if (_allPrefabs != null && _allPrefabs.Count > SelGridInt && _allPrefabs[SelGridInt] != null)
		{
			GizmoTile = Instantiate(_allPrefabs[SelGridInt]);
			Undo.RegisterCreatedObjectUndo(GizmoTile, "CreatedTile");
		}
		else
		{
			GizmoTile = new GameObject();
			Undo.RegisterCreatedObjectUndo(GizmoTile, "CreatedTile");
		}
		/*
		if (_allPrefabs != null)
		{
			RotationModeStatic = (Mathf.FloorToInt(_allPrefabs[SelGridInt].transform.rotation.eulerAngles.z / 90)) % 4;
			RotationState = true;
		}
		*/
		GizmoTile.name = "GizmoTile";
		GizmoTile.hideFlags = HideFlags.HideInHierarchy;
		if (_gizmoTileSR == null)
			_gizmoTileSR = GizmoTile.GetComponent<SpriteRenderer>();

        var option = GizmoTile.GetComponent<PrefabOption>();
        if (option)
        {
            if (GizmoCursor)
            {
                var cursor = GizmoCursor.GetComponent<SpriteRenderer>();
                cursor.sprite = FullCursor;
			}
        }
        
		//make it transparent
		MakeGhost(GizmoTile);
	}


	#region prefab selection shortcuts
	//Select object by pressing the F keys
	/*
	[MenuItem("Level Editor/Select GameObject 1 _F1")]
	private static void Sel1()
	{
		if (Instance == null) return;
		if (_allPrefabs.Count <= 0) return;
		SelGridIntStatic = 0;
		SelGridIntState = true;
	}

	[MenuItem("Level Editor/Select GameObject 2 _F2")]
	private static void Sel2()
	{
		if (Instance == null) return;
		if (_allPrefabs.Count <= 1) return;
		SelGridIntStatic = 1;
		SelGridIntState = true;
	}

	[MenuItem("Level Editor/Select GameObject 3 _F3")]
	private static void Sel3()
	{
		if (Instance == null) return;
		if (_allPrefabs.Count <= 2) return;
		SelGridIntStatic = 2;
		SelGridIntState = true;
	}

	[MenuItem("Level Editor/Select GameObject 4 _F4")]
	private static void Sel4()
	{
		if (Instance == null) return;
		if (_allPrefabs.Count <= 3) return;
		SelGridIntStatic = 3;
		SelGridIntState = true;
	}

	[MenuItem("Level Editor/Select GameObject 5 _F5")]
	private static void Sel5()
	{
		if (Instance == null) return;
		if (_allPrefabs.Count <= 4) return;
		SelGridIntStatic = 4;
		SelGridIntState = true;
	}
	*/
	#endregion

	#region Layer functions

	//add a layer when + button is pressed
	private void AddLayer(ReorderableList list)
	{
		//holds highest layer value
		int highestLayer = -1;

		//go through all layers finds highest numbered layer (just like in Photoshop)
		foreach (var t in LayerList)
		{
			if (t.name.Length <= 6 || !t.name.Substring(0, 6).Contains("Layer ")) continue;
			if (t.name.Length <= 5) continue;
			//parses number after the Layer
			var val = int.Parse(t.name.Substring(6));

			if (val > highestLayer)
				highestLayer = val;
		}
		highestLayer++;

		//creates layer
		ShowLog("Layer Creation");
		GameObject layerGo = new GameObject("Layer " + highestLayer);
		Undo.RegisterCreatedObjectUndo(layerGo, "Created layer");
		layerGo.AddComponent<Layer>();
		Layer layercmp = layerGo.GetComponent<Layer>();

		layercmp.id = 5;

		//sets current layer
		if (_currentLayer != null)
		{
			LayerList.Insert(LayerList.IndexOf(_currentLayer), layercmp);
			_currentLayer = layercmp;
			LayerReorder.index = LayerList.IndexOf(_currentLayer);

		}
		else
		{
			LayerList.Add(layercmp);
			LayerReorder.index = 0;
		}
		OrganizeLayers(list);
	}

	//remove layer from list and hierarchy
	private void RemoveLayer(ReorderableList list)
	{
		Undo.DestroyObjectImmediate(LayerList[list.index].gameObject);
		//DestroyImmediate (LayerList [list.index].gameObject);
		LayerList.RemoveAt(list.index);
	}

	/// Loads the layers from the scene
	void LoadLayers()
	{
		//first clears the list
		LayerList?.Clear();

		//finds the layers and adds them to the list
		foreach (var item in FindObjectsOfType<Layer>())
		{
			LayerList?.Add(item);
		}

		LayerList?.Sort((x, y) => x.transform.GetSiblingIndex() < y.transform.GetSiblingIndex() ? -1 : 1);

        FloorLayer = FindObjectOfType<Floor>();
    }

	//Reorder layers in list according to their order in the hierarchy
	void ReorderLayers()
	{
		LayerList.Sort((x, y) => x.transform.GetSiblingIndex() < y.transform.GetSiblingIndex() ? -1 : 1);
		OrganizeLayers(LayerReorder);
	}

	private void OrganizeLayers(ReorderableList list)
	{
		//goes through all layers
		for (var i = 0; i < LayerList.Count; i++)
		{

			//sets priority correctly
			LayerList[i].priority = LayerList.Count - i;

			LayerList[i].transform.SetSiblingIndex(i);

			//sets order in layer correctly
			for (var k = 0; k < LayerList[i].transform.childCount; k++)
			{

				//ShowLog ("List was reordered");
				//Undo.SetTransformParent(LayerList[i].transform,null,"Moved Lyers");
				var sr = LayerList[i].transform.GetChild(k).GetComponent<SpriteRenderer>();

				if (!sr) sr.sortingOrder = LayerList.Count - i;
			}
			list.index = LayerList.IndexOf(_currentLayer);
		}

		//if there are no layers, create one
		if (LayerList.Count == 0)
		{
			AddLayer(LayerReorder);
			_currentLayer = LayerList[0];
			LayerReorder.index = 0;
		}
	}

	private void SelectLayer(ReorderableList list)
	{
		_currentLayer = LayerList[list.index];

		if (Selection.activeGameObject == null || Selection.activeGameObject == _currentLayer.gameObject) return;
		var l = Selection.activeGameObject.GetComponent<Layer>();
		ShowLog("SelectedLayer");
		if (l == null) return;
		Selection.activeGameObject = _currentLayer.gameObject;
		ShowLog(Selection.activeGameObject.name);
	}

	private void DrawHeader(Rect rect)
	{
		GUI.Label(rect, "Layers");
	}

	//Draws reorderable list element
	private void DrawElement(Rect rect, int index, bool active, bool focused)
	{
		//fetches corresponding layer
		var item = LayerList[index];

		//prevents errors
		if (!item) return;

		EditorGUI.BeginChangeCheck();
		var layerName = item.name;
		//Editable name
		layerName = EditorGUI.TextField(new Rect(rect.x, rect.y, 170, EditorGUIUtility.singleLineHeight), layerName);

		if (EditorGUI.EndChangeCheck())
		{
			Undo.RegisterCompleteObjectUndo(item.gameObject, "Layer Name Changed");
			item.gameObject.name = layerName;
			//Undo.RegisterCompleteObjectUndo(item.gameObject, "Layer Name Changed");
		}

		//Uneditable gameobject
		EditorGUI.ObjectField(new Rect(rect.x + 175, rect.y, rect.width - rect.width + 35, EditorGUIUtility.singleLineHeight), item.gameObject, typeof(GameObject), true);
	}
	#endregion
}

#endif
