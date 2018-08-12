﻿
using UnityEngine;
using UnityEngine.EventSystems;

public class RaiseShieldsButton : Button
{
	public override string GetLabel()
	{
		return "Raise Shields";
	}

	public override bool Execute()
	{
		PlayerData playerData = PersistentController.m_instance.m_playerData;

		if ( playerData.m_shipConfiguration.m_shieldingClass == 0 )
		{
			m_spaceflightController.m_uiSoundController.Play( UISoundController.UISound.Error );

			m_spaceflightController.m_messages.text = "Ship is not equipped with shields.";

			m_spaceflightController.m_buttonController.UpdateButtonSprites();

			return false;
		}

		return false;
	}
}
