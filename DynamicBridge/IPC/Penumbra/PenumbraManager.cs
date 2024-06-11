﻿using Dalamud.Game.ClientState.Objects.Types;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using Penumbra.Api.Enums;
using Penumbra.Api.IpcSubscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.IPC.Penumbra;
public class PenumbraManager
{
    Guid? OldAssignment = null;

    public PenumbraManager()
    {
        EzIPC.Init(this, "Penumbra");
    }

    GetCollections GetCollections = new(Svc.PluginInterface);
    public IEnumerable<string> GetCollectionNames()
    {
        try
        {
            return GetCollections.Invoke().Select(x => x.Value);
        }
        catch(Exception e)
        {
            e.Log();
            return []; 
        }
    }

    public Guid GetGuidForCollection(string collectionName)
    {
        try
        {
            return new GetCollectionsByIdentifier(Svc.PluginInterface).Invoke(collectionName).First().Id;
        }
        catch(Exception e)
        {
            e.Log();
        }
        return default;
    }

    public void SetAssignment(string newAssignment)
    {
        try
        {
            var result = new SetCollectionForObject(Svc.PluginInterface).Invoke(0, GetGuidForCollection(newAssignment), true, true);
            if (!result.Item1.EqualsAny(PenumbraApiEc.Success, PenumbraApiEc.NothingChanged))
            {
                var e = $"Error setting Penumbra assignment: {result.Item1}";
                PluginLog.Error(e);
                Notify.Error(e);
            }
            else
            {
                OldAssignment ??= result.OldCollection?.Id;
                if(result.Item1 == PenumbraApiEc.Success) P.TaskManager.Enqueue(RedrawLocalPlayer);
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    public void UnsetAssignmentIfNeeded()
    {
        if (OldAssignment == null) return;
        try
        {
            var result = new SetCollectionForObject(Svc.PluginInterface).Invoke(0, OldAssignment, true, true);
            OldAssignment = null;
            if (result.Item1 == PenumbraApiEc.Success) P.TaskManager.Enqueue(RedrawLocalPlayer);
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    public void RedrawLocalPlayer()
    {
        try
        {
            new RedrawObject(Svc.PluginInterface).Invoke(0);
        }
        catch(Exception e)
        {
            e.Log();
        }
    }
}
