﻿
using UnityEngine;

public class LaunchYesButton : ShipButton
{
	// whether or not we have started the countdown
	bool m_countdownStarted;

	// keep track of what countdown number we are showing
	int m_lastCountdownNumberShown;

	// our launch timer
	float m_timer;

	public override string GetLabel()
	{
		return "Yes";
	}

	public override bool Execute()
	{
		// get to the player data
		PlayerData playerData = DataController.m_instance.m_playerData;

		if ( playerData.m_starflight.m_location == Starflight.Location.DockingBay )
		{
			// update the messages log
			m_spaceflightController.m_spaceflightUI.ChangeMessageText( "Opening docking bay doors..." );

			// configure the infinite starfield system to become visible at lower speeds
			m_spaceflightController.m_player.SetStarfieldFullyVisibleSpeed( 5.0f );

			// reset the last countdown number shown
			m_lastCountdownNumberShown = 0;

			if ( !m_spaceflightController.m_skipCinematics )
			{
				// open the docking bay doors
				m_spaceflightController.m_dockingBay.OpenDockingBayDoors();

				// play the launch sound
				SoundController.m_instance.PlaySound( SoundController.Sound.Launch );
			}
		}
		else
		{
			// TODO: Launching from planet cinematics
		}

		// reset the timer
		m_timer = 0.0f;

		// we haven't started the countdown just yet
		m_countdownStarted = false;

		// stop the music
		MusicController.m_instance.ChangeToTrack( MusicController.Track.None );

		return true;
	}

	public override bool Update()
	{
		// keep track of the cutscene time
		m_timer += Time.deltaTime * ( m_spaceflightController.m_skipCinematics ? 20.0f : 1.0f );

		// at 15 seconds begin the countdown...
		if ( m_timer >= 15.0f )
		{
			// check if we have have not played the countdown sound yet
			if ( !m_countdownStarted )
			{
				if ( !m_spaceflightController.m_skipCinematics )
				{
					// play the countdown sound
					SoundController.m_instance.PlaySound( SoundController.Sound.Countdown );
				}

				// remember that we've already started playing the countdown sound
				m_countdownStarted = true;
			}

			// do countdown text animation
			int currentNumber = Mathf.FloorToInt( 21.5f - m_timer );

			// check if we are showing the countdown text
			if ( ( currentNumber >= 1 ) && ( currentNumber <= 5 ) )
			{
				if ( currentNumber != m_lastCountdownNumberShown )
				{
					m_lastCountdownNumberShown = currentNumber;

					m_spaceflightController.m_spaceflightUI.SetCountdownText( currentNumber.ToString() );
				}
			}
			else
			{
				// have we reached zero in the countdown?
				if ( currentNumber <= 0 )
				{
					// get to the player data
					PlayerData playerData = DataController.m_instance.m_playerData;

					// are we in the docking bay?
					if ( playerData.m_starflight.m_location == Starflight.Location.DockingBay )
					{
						// yes - update the messages text
						m_spaceflightController.m_spaceflightUI.ChangeMessageText( "Leaving starport..." );

						// figure out how much to move the ship forward by (with an exponential acceleration curve)
						float y = ( m_timer - 20.5f ) * 5.0f;

						y *= y;

						// have we reached the end of the launch trip?
						if ( y >= 1024.0f )
						{
							// update the player location
							playerData.m_starflight.m_location = Starflight.Location.JustLaunched;

							// switch modes
							m_spaceflightController.SwitchMode();

							// yep - fix us at exactly 1024 units above the zero plane
							y = 1024.0f;

							// force the planet controller for the arth station to update now so all the planets are in the correct position
							m_spaceflightController.m_systemController.m_planetController[ 2 ].ForceUpdate();

							// calculate the new position of the player (just north of the arth spaceport)
							Vector3 newPosition = m_spaceflightController.m_systemController.m_planetController[ 2 ].transform.position;
							newPosition.y = 0.0f;
							newPosition.z += 128.0f;

							// update the player's system coordinates to the new position
							playerData.m_starflight.m_systemCoordinates = newPosition;

							// restore the bridge buttons (this also ends the launch function)
							m_spaceflightController.m_buttonController.RestoreBridgeButtons();
						}

						// update the position of the camera
						m_spaceflightController.m_player.DollyCamera( 2048.0f - y );

						// fade the map out
						m_spaceflightController.m_spaceflightUI.FadeMap( 1.0f - ( y / 1024.0f ) );
					}
					else
					{
						// TODO: Launching from planet animation
					}
				}
			}
		}

		// returning true prevents the default spaceflight update from running
		return true;
	}
}
