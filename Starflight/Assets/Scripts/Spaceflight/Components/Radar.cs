﻿
using UnityEngine;
using System;

public class Radar : MonoBehaviour
{
	class Detection
	{
		public PD_Encounter m_encounter;
		public float m_timeSinceDetection;
		public SVGImage m_blip;
		public float m_initialOpacity;

		public Detection( SVGImage blip )
		{
			m_encounter = null;
			m_timeSinceDetection = 3600.0f;
			m_blip = blip;
			m_initialOpacity = 0.0f;
		}
	}

	public SVGImage m_ring;
	public SVGImage m_sweep;
	public SVGImage[] m_blips;

	float m_sweepAngle;

	Detection[] m_detectionList;

	// unity start
	void Start()
	{
		// clone material and make them all invisible
		m_ring.material = new Material( m_ring.material );

//		m_ring.material.SetColor( "AlbedoColor", new Color( 1, 1, 1, 0 ) );

		m_sweep.material = new Material( m_sweep.material );

//		m_sweep.material.SetColor( "AlbedoColor", new Color( 1, 1, 1, 0 ) );

		foreach ( var blip in m_blips )
		{
			blip.material = new Material( blip.material );

			blip.material.SetColor( "AlbedoColor", new Color( 1, 1, 1, 0 ) );
		}

		// start sweep angle at zero
		m_sweepAngle = 0.0f;

		// allocate detection list
		m_detectionList = new Detection[ m_blips.Length ];

		for ( var i = 0; i < m_detectionList.Length; i++ )
		{
			m_detectionList[ i ] = new Detection( m_blips[ i ] );
		}
	}

	// unity update
	void Update()
	{
		// get to the player data
		var playerData = DataController.m_instance.m_playerData;

		// radar is visible only in hyperspace or in the star system
		if ( ( playerData.m_general.m_location != PD_General.Location.Hyperspace ) && ( playerData.m_general.m_location != PD_General.Location.StarSystem ) )
		{
			// hide the radar outline
//			m_ring.material.SetColor( "AlbedoColor", new Color( 1, 1, 1, 0 ) );

			// nothing more to do here
			return;
		}

		// rotate the sweep (60 degrees per second, 6 seconds = full sweep)
		m_sweepAngle -= Time.deltaTime * 60.0f;

		if ( m_sweepAngle <= -180.0f )
		{
			m_sweepAngle += 360.0f;
		}

		m_sweep.rectTransform.localRotation = Quaternion.Euler( 0.0f, 0.0f, m_sweepAngle );

		// update detection list - drop out detections older than 6 seconds
		foreach ( var detection in m_detectionList )
		{
			detection.m_timeSinceDetection += Time.deltaTime;

			if ( detection.m_timeSinceDetection >= 6.0f )
			{
				detection.m_timeSinceDetection = 6.0f;

				detection.m_encounter = null;

				detection.m_blip.material.SetColor( "AlbedoColor", new Color( 1, 1, 1, 0 ) );
			}
			else if ( detection.m_timeSinceDetection >= 3.0f )
			{
				var opacity = Mathf.Lerp( detection.m_initialOpacity, 0.0f, ( detection.m_timeSinceDetection - 3.0f ) / 3.0f );

				detection.m_blip.material.SetColor( "AlbedoColor", new Color( 1, 1, 1, opacity ) );
			}
		}

		// figure out which coordinate to use for encounter distances
		var coordinates = ( playerData.m_general.m_location == PD_General.Location.Hyperspace ) ? playerData.m_general.m_hyperspaceCoordinates : playerData.m_general.m_starSystemCoordinates;

		// go through each potential encounter
		foreach ( var encounter in playerData.m_encounterList )
		{
			// update the distance to the encounter
			encounter.Update( playerData.m_general.m_location, playerData.m_general.m_currentStarId, coordinates );
		}

		// sort the results
		Array.Sort( playerData.m_encounterList );

		// go through each potential encounter
		foreach ( var encounter in playerData.m_encounterList )
		{
			// are they close enough for us to detect them?
			if ( encounter.GetDistance() > 2048.0f )
			{
				// no - stop now (the list is sorted so all remaining encounters are further away)
				break;
			}

			// calculate the direction of the encounter relative to the player
			var encounterDirection = encounter.m_coordinates - coordinates;

			// calculate the angle of the encounter
			var angle = Vector3.SignedAngle( Vector3.forward, encounterDirection, Vector3.down );

			// is it close to our current sweep direction for this frame?
			if ( ( angle > m_sweepAngle ) && ( angle < ( m_sweepAngle + 15.0f ) ) )
			{
				// yes - go through detection list
				bool ignoreEncounter = false;

				foreach ( var detection in m_detectionList )
				{
					// is this an active detection?
					if ( detection.m_encounter == encounter )
					{
						// yes - did we recently detect this?
						if ( detection.m_timeSinceDetection < 3.0f )
						{
							// yes - just reset the time since detection and ignore this encounter
							detection.m_timeSinceDetection = 0.0f;

							ignoreEncounter = true;

							break;
						}
					}
				}

				if ( ignoreEncounter )
				{
					continue;
				}

				// detection slot to use
				Detection detectionToUse = null;

				// go through detection list
				foreach ( var detection in m_detectionList )
				{
					// is this detection already in our list?
					if ( detection.m_encounter == encounter )
					{
						// yes - just update the same one
						detectionToUse = detection;
						break;
					}
				}

				// still need to find a detection slot?
				if ( detectionToUse == null )
				{
					// yes - go through the detection list again
					foreach ( var detection in m_detectionList )
					{
						// is this slot available?
						if ( detection.m_encounter == null )
						{
							// yes - use this one
							detectionToUse = detection;
							break;
						}
					}
				}

				// did we find a slot to use?
				if ( detectionToUse != null )
				{
					// calculate the blip opacity
					var opacity = Mathf.Lerp( 0.3f, 1.0f, 1.0f - ( encounter.GetDistance() / 2048.0f ) );

					// yes - update the blip material
					detectionToUse.m_blip.material.SetColor( "AlbedoColor", new Color( 1, 1, 1, opacity ) );

					// set the rotation (position really) of the blip
					detectionToUse.m_blip.rectTransform.localRotation = Quaternion.Euler( 0.0f, 0.0f, angle );

					// reset time since detection
					detectionToUse.m_timeSinceDetection = 0.0f;

					// remember the encounter
					detectionToUse.m_encounter = encounter;

					// remember the opacity
					detectionToUse.m_initialOpacity = opacity;

					// play the radar blip sound
					SoundController.m_instance.PlaySound( SoundController.Sound.RadarBlip, opacity * 0.5f );
				}
			}
		}
	}
}
