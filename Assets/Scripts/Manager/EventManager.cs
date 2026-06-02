using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameEvent
{
    GameStarted,
    // 之後有其他事件繼續加在這裡
}

public  class EventManager: MonoBehaviour
{
    private static readonly Dictionary<GameEvent, Action> eventTable
        = new Dictionary<GameEvent, Action>();

    public static void Subscribe(GameEvent gameEvent, Action listener)
    {
        if (!eventTable.ContainsKey(gameEvent))
            eventTable[gameEvent] = null;
        eventTable[gameEvent] += listener;
    }

    public static void Unsubscribe(GameEvent gameEvent, Action listener)
    {
        if (eventTable.ContainsKey(gameEvent))
            eventTable[gameEvent] -= listener;
    }

    public static void Publish(GameEvent gameEvent)
    {
        if (eventTable.ContainsKey(gameEvent))
            eventTable[gameEvent]?.Invoke();
    }
}