﻿//  Copyright 2011-2013 Marc Fletcher, Matthew Dean
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
//  A commercial license of this software can also be purchased. 
//  Please see <http://www.networkcomms.net/licensing/> for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkCommsDotNet;
using ProtoBuf;

namespace DistributedFileSystem
{
    /// <summary>
    /// Used to classify the different types of ChunkAvailabilityReply in response to a ChunkAvailabilityRequest
    /// </summary>
    public enum ChunkReplyState : byte
    {
        /// <summary>
        /// Specifies that data will be included.
        /// </summary>
        DataIncluded,

        /// <summary>
        /// The item or requested chunk is not available
        /// </summary>
        ItemOrChunkNotAvailable,

        /// <summary>
        /// The contacted peer is currently busy, please try again later.
        /// </summary>
        PeerBusy
    }

    /// <summary>
    /// Wrapper used for requesting a chunk
    /// </summary>
    [ProtoContract]
    public class ChunkAvailabilityRequest
    {
        /// <summary>
        /// The checksum of the item being requested
        /// </summary>
        [ProtoMember(1)]
        public string ItemCheckSum { get; private set; }

        /// <summary>
        /// The index of the requested chunk
        /// </summary>
        [ProtoMember(2)]
        public byte ChunkIndex { get; private set; }

        /// <summary>
        /// The time this request was created
        /// </summary>
        public DateTime RequestCreationTime { get; private set; }

        /// <summary>
        /// The peer contacted for this request
        /// </summary>
        public ConnectionInfo PeerConnectionInfo { get; private set; }

        /// <summary>
        /// We are currently processing incoming data for this request.
        /// </summary>
        public bool RequestIncoming { get; set; }

        /// <summary>
        /// We have received data and this request is complete.
        /// </summary>
        public bool RequestComplete { get; set; }

        private ChunkAvailabilityRequest() { }

        /// <summary>
        /// Instantiate a new ChunkAvailabilityRequest
        /// </summary>
        /// <param name="itemCheckSum">The checksum of the DFS item</param>
        /// <param name="chunkIndex">The index of the requested chunk</param>
        /// <param name="peerConnectionInfo">The peer contacted for this request</param>
        public ChunkAvailabilityRequest(string itemCheckSum, byte chunkIndex, ConnectionInfo peerConnectionInfo)
        {
            this.ItemCheckSum = itemCheckSum;
            this.ChunkIndex = chunkIndex;
            this.RequestCreationTime = DateTime.Now;
            this.PeerConnectionInfo = peerConnectionInfo;
            this.RequestIncoming = false;
            this.RequestComplete = false;
        }
    }

    /// <summary>
    /// A wrapper used to reply to a ChunkAvailabilityRequest
    /// </summary>
    [ProtoContract]
    public class ChunkAvailabilityReply
    {
        /// <summary>
        /// The checksum of the item being requested
        /// </summary>
        [ProtoMember(1)]
        public string ItemCheckSum { get; private set; }

        /// <summary>
        /// The index of the requested chunk
        /// </summary>
        [ProtoMember(2)]
        public byte ChunkIndex { get; private set; }

        /// <summary>
        /// The state of this reply
        /// </summary>
        [ProtoMember(3)]
        public ChunkReplyState ReplyState { get; private set; }

        /// <summary>
        /// The sequence number used to send the chunk data
        /// </summary>
        [ProtoMember(4)]
        public long DataSequenceNumber { get; private set; }

        /// <summary>
        /// The network identifier of the peer that generated this ChunkAvailabilityReply
        /// </summary>
        [ProtoMember(5)]
        public string SourceNetworkIdentifier { get; private set; }

        /// <summary>
        /// The connectionInfo of the peer that generated this ChunkAvailabilityReply
        /// </summary>
        public ConnectionInfo SourceConnectionInfo { get; private set; }

        /// <summary>
        /// The requested data
        /// </summary>
        public byte[] ChunkData { get; private set; }

        /// <summary>
        /// True once ChunkData has been set
        /// </summary>
        public bool ChunkDataSet { get; private set; }

        private ChunkAvailabilityReply() { }

        /// <summary>
        /// Create an ChunkAvailabilityReply which will not contain the requested data.
        /// </summary>
        /// <param name="sourceNetworkIdentifier">The network identifier of the source of this ChunkAvailabilityReply</param>
        /// <param name="itemCheckSum">The checksum of the DFS item</param>
        /// <param name="chunkIndex">The chunkIndex of the requested item</param>
        /// <param name="replyState">A suitable reply state</param>
        public ChunkAvailabilityReply(string sourceNetworkIdentifier, string itemCheckSum, byte chunkIndex, ChunkReplyState replyState)
        {
            this.SourceNetworkIdentifier = sourceNetworkIdentifier;
            this.ItemCheckSum = itemCheckSum;
            this.ChunkIndex = chunkIndex;
            this.ReplyState = replyState;
        }

        /// <summary>
        /// Create an ChunkAvailabilityReply which will contain the requested data.
        /// </summary>
        /// <param name="sourceNetworkIdentifier">The network identifier of the source of this ChunkAvailabilityReply</param>
        /// <param name="itemCheckSum">The checksum of the DFS item</param>
        /// <param name="chunkIndex">The chunkIndex of the requested item</param>
        /// <param name="dataSequenceNumber">The packet sequence number used to send the data</param>
        public ChunkAvailabilityReply(string sourceNetworkIdentifier, string itemCheckSum, byte chunkIndex, long dataSequenceNumber)
        {
            this.SourceNetworkIdentifier = sourceNetworkIdentifier;
            this.ItemCheckSum = itemCheckSum;
            this.ChunkIndex = chunkIndex;
            this.DataSequenceNumber = dataSequenceNumber;
            this.ReplyState = ChunkReplyState.DataIncluded;
        }

        /// <summary>
        /// Set the data for this ChunkAvailabilityReply
        /// </summary>
        /// <param name="chunkData">The chunk data</param>
        public void SetChunkData(byte[] chunkData)
        {
            this.ChunkData = chunkData;
            ChunkDataSet = true;
        }

        /// <summary>
        /// Set the connectionInfo associated with the source of this ChunkAvailabilityReply
        /// </summary>
        /// <param name="info">The ConnectionInfo associated with the source of this ChunkAvailabilityReply</param>
        public void SetSourceConnectionInfo(ConnectionInfo info)
        {
            this.SourceConnectionInfo = info;
        }
    }

    /// <summary>
    /// Temporary storage for chunk data which is awaiting corresponding ChunkAvailabilityReply
    /// </summary>
    class ChunkDataWrapper
    {
        /// <summary>
        /// The packet sequence number of the chunk data
        /// </summary>
        public long IncomingSequenceNumber { get; private set; }

        /// <summary>
        /// The chunk data
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// The time this chunk data was received
        /// </summary>
        public DateTime TimeCreated { get; private set; }

        /// <summary>
        /// The ChunkAvailabilityReply associated with this chunk data
        /// </summary>
        public ChunkAvailabilityReply ChunkAvailabilityReply { get; private set; }

        /// <summary>
        /// Initialise a ChunkDataWrapper when the ChunkAvailabilityReply is received before associated data.
        /// </summary>
        /// <param name="chunkAvailabilityReply">The matching ChunkAvailabilityReply</param>
        public ChunkDataWrapper(ChunkAvailabilityReply chunkAvailabilityReply)
        {
            if (chunkAvailabilityReply == null)
                throw new Exception("Unable to create a ChunkDataWrapper with a null ChunkAvailabilityReply reference.");

            this.ChunkAvailabilityReply = chunkAvailabilityReply;
            this.TimeCreated = DateTime.Now;
        }

        /// <summary>
        /// Initialise a ChunkDataWrapper when the data is received before the associated ChunkAvailabilityReply.
        /// </summary>
        /// <param name="incomingSequenceNumber">The packet sequence number of the chunk data</param>
        /// <param name="data">The chunk data</param>
        public ChunkDataWrapper(long incomingSequenceNumber, byte[] data)
        {
            this.IncomingSequenceNumber = incomingSequenceNumber;
            this.Data = data;
            this.TimeCreated = DateTime.Now;
        }
    }
}
