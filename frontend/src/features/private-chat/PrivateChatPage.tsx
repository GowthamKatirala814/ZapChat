import { MessageSquare } from "lucide-react";
import { useState } from "react";
import toast from "react-hot-toast";
import { useNavigate, useParams } from "react-router-dom";
import { EmptyState } from "../../components/feedback";
import { ListDetail } from "../../components/layout/ListDetail";
import { Button } from "../../components/ui";
import { paths } from "../../config";
import { errorMessage } from "../../services/api";
import { ConversationList } from "./ConversationList";
import { ConversationView } from "./ConversationView";
import { NewConversationDialog } from "./NewConversationDialog";
import { useConversations, usePrivateChatMutations, usePrivateChatRealtime } from "./usePrivateChat";

export function PrivateChatPage() {
  const { conversationId } = useParams<{ conversationId: string }>();
  const navigate = useNavigate();

  const conversations = useConversations();
  const { start } = usePrivateChatMutations();
  const connection = usePrivateChatRealtime(conversationId);

  const [showNew, setShowNew] = useState(false);

  return (
    <>
      <ListDetail
        listLabel="Conversations"
        hasDetail={Boolean(conversationId)}
        list={
          <ConversationList
            conversations={conversations.data}
            isLoading={conversations.isLoading}
            error={conversations.error}
            onRetry={() => void conversations.refetch()}
            activeId={conversationId}
            onNew={() => setShowNew(true)}
          />
        }
        detail={
          conversationId ? (
            <ConversationView
              key={conversationId}
              conversationId={conversationId}
              connection={connection}
            />
          ) : (
            <div className="flex-1 flex items-center justify-center">
              <EmptyState
                icon={<MessageSquare size={20} />}
                title="Your private conversations"
                description="Messages here are visible only to you and the person you are talking to. Both of you appear under anonymous names."
                action={
                  <Button size="sm" variant="secondary" onClick={() => setShowNew(true)}>
                    Start a conversation
                  </Button>
                }
              />
            </div>
          )
        }
      />

      <NewConversationDialog
        open={showNew}
        onClose={() => setShowNew(false)}
        isStarting={start.isPending}
        onSelect={(userId) =>
          start.mutate(userId, {
            onSuccess: (conversation) => {
              setShowNew(false);
              // Idempotent server-side, so picking someone you already talk to opens
              // the existing conversation rather than creating a duplicate.
              navigate(paths.conversation(conversation.id));
            },
            onError: (error) =>
              toast.error(errorMessage(error, "The conversation could not be started.")),
          })
        }
      />
    </>
  );
}
