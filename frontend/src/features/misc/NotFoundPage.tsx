import { Compass } from "lucide-react";
import { Link } from "react-router-dom";
import { EmptyState } from "../../components/feedback";
import { Button } from "../../components/ui";
import { paths } from "../../config";

export function NotFoundPage() {
  return (
    <div className="flex-1 flex items-center justify-center p-6">
      <EmptyState
        icon={<Compass size={20} />}
        title="This page does not exist"
        description="The link may be out of date, or the page may have been moved."
        action={
          <Link to={paths.chat}>
            <Button variant="secondary" size="sm">
              Go to channels
            </Button>
          </Link>
        }
      />
    </div>
  );
}
