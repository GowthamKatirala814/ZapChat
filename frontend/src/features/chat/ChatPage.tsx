import { MessagesSquare } from "lucide-react";
import { EmptyState } from "../../components/feedback";
import { ListDetail } from "../../components/layout/ListDetail";
import { useParams } from "react-router-dom";
import { RoomList } from "./RoomList";
import { RoomView } from "./RoomView";
import { useChatRealtime, useRooms } from "./useChat";

/**
 * Channels.
 *
 * The hub subscription lives here rather than inside `RoomView`, so it survives moving
 * between rooms — one connection for the whole feature, joining and leaving groups as
 * the open room changes.
 */
export function ChatPage() {
  const { roomId } = useParams<{ roomId: string }>();
  const rooms = useRooms();

  const connection = useChatRealtime(roomId);

  return (
    <ListDetail
      listLabel="Channels"
      hasDetail={Boolean(roomId)}
      list={
        <RoomList
          rooms={rooms.data}
          isLoading={rooms.isLoading}
          error={rooms.error}
          onRetry={() => void rooms.refetch()}
          activeRoomId={roomId}
        />
      }
      detail={
        roomId ? (
          <RoomView key={roomId} roomId={roomId} connection={connection} />
        ) : (
          <div className="flex-1 flex items-center justify-center">
            <EmptyState
              icon={<MessagesSquare size={20} />}
              title="Pick a channel"
              description="Everything you post here carries your anonymous name — never your real one."
            />
          </div>
        )
      }
    />
  );
}
