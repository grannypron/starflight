﻿
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class SpaceflightController : MonoBehaviour
{
	// set this to true to skip cinematics
	public bool m_skipCinematics;
	public bool m_forceResetToDockingBay;

	// set this to the maximum speed of the ship
	public float m_maximumShipSpeedHyperspace;
	public float m_maximumShipSpeedStarSystem;
	public float m_timeToReachMaximumShipSpeed;
	public float m_timeToStop;

	// the different components of the spaceflight scene
	public Player m_player;
	public DockingBay m_dockingBay;
	public JustLaunched m_justLaunched;
	public StarSystem m_starSystem;
	public Hyperspace m_hyperspace;
	public SpaceflightUI m_spaceflightUI;

	// controllers
	public ButtonController m_buttonController { get; protected set; }
	public DisplayController m_displayController { get; protected set; }
	public SystemController m_systemController { get; protected set; }

	// save game timer
	float m_timer;

	// unity awake
	void Awake()
	{
		// check if we loaded the persistent scene
		if ( DataController.m_instance == null )
		{
			// nope - so then do it now and tell it to skip the intro scene
			DataController.m_sceneToLoad = "Spaceflight";

			SceneManager.LoadScene( "Persistent" );
		}
		else
		{
			// get access to the various controllers
			m_buttonController = GetComponent<ButtonController>();
			m_displayController = GetComponent<DisplayController>();
			m_systemController = GetComponent<SystemController>();
		}
	}

	// unity start
	void Start()
	{
		// turn off controller navigation of the UI
		EventSystem.current.sendNavigationEvents = false;

		// reset the player to the docking bay if we want that to happen
		if ( m_forceResetToDockingBay )
		{
			PlayerData playerData = DataController.m_instance.m_playerData;
			playerData.m_starflight.m_hyperspaceCoordinates = Tools.GameToWorldCoordinates( new Vector3( 125.0f, 0.0f, 101.0f ) );
			playerData.m_starflight.m_location = Starflight.Location.DockingBay;
		}

		// switch to the correct mode
		SwitchMode();

		// fade in the scene
		SceneFadeController.m_instance.FadeIn();

		// reset the save game timer
		m_timer = 0.0f;
	}

	// unity update
	void Update()
	{
		// update the game time
		PlayerData playerData = DataController.m_instance.m_playerData;

		playerData.m_starflight.UpdateGameTime( Time.deltaTime );

		// save the game once in a while
		m_timer += Time.deltaTime;

		if ( m_timer >= 30.0f )
		{
			m_timer -= 30.0f;

			DataController.m_instance.SavePlayerData();
		}
	}

	// call this to switch to the correct mode
	public void SwitchMode()
	{
		// hide all components
		m_player.Hide();
		m_dockingBay.Hide();
		m_starSystem.Hide();
		m_hyperspace.Hide();

		// make sure the map is visible
		m_spaceflightUI.FadeMap( 1.0f );

		// get to the player data
		PlayerData playerData = DataController.m_instance.m_playerData;

		// switch to the correct mode
		Debug.Log( "Switching to " + playerData.m_starflight.m_location );

		switch ( playerData.m_starflight.m_location )
		{
			case Starflight.Location.DockingBay:
				m_dockingBay.Show();
				break;
			case Starflight.Location.JustLaunched:
				m_justLaunched.Show();
				break;
			case Starflight.Location.StarSystem:
				m_starSystem.Show();
				break;
			case Starflight.Location.Hyperspace:
				m_hyperspace.Show();
				break;
		}

		// save the player data (since the location was most likely changed)
		DataController.m_instance.SavePlayerData();
	}

	// call this to update the player's position
	public void UpdatePlayerPosition( Vector3 newPosition )
	{
		// update the game object
		m_player.SetPosition( newPosition );

		// get to the player data
		PlayerData playerData = DataController.m_instance.m_playerData;

		// update the player data (it will save out to disk eventually)
		if ( playerData.m_starflight.m_location != Starflight.Location.Hyperspace )
		{
			playerData.m_starflight.m_systemCoordinates = newPosition;
		}
		else
		{
			playerData.m_starflight.m_hyperspaceCoordinates = newPosition;
		}
	}
}
