import {Link, useParams} from "react-router";
import {
    Tabs,
    TabsContent,
    TabsList,
    TabsTrigger,
} from "@/components/ui/tabs"
import {ScrollArea} from "@/components/ui/scroll-area.tsx";
import {getListUserChoreOccurrencesQueryKey, useGetUser, useListUserChoreOccurrences} from "@/api/users/users.ts";
import {ArrowLeft, Check, ClockPlus} from "lucide-react";
import {
    Card,
    CardContent,
    CardDescription,
    CardFooter,
    CardHeader,
    CardTitle,
} from "@/components/ui/card"
import {Button} from "@/components/ui/button.tsx";
import {useCompleteChore, useSnoozeChore} from "@/api/chore-occurrences/chore-occurrences.ts";
import {useQueryClient} from "@tanstack/react-query";

function useCurrentUserId() {
    const { userId } = useParams<{ userId: string }>();
    return Number(userId);
}

function User() {
    const userId = useCurrentUserId();
    // Update current user in a react context or some shit
    const {data: userData, isPending} = useGetUser(userId);

    const user = userData?.data;

    if (isPending) {
        return <div>Loading...</div>
    }

    if (!user) {
        return <div>User not found</div>
    }

    return (
        <>
            <div className="flex flex-row gap-15">
                <Link to="/">
                    <Button className="bg-secondary text-primary">
                        <ArrowLeft className="left-6"/>
                        Back
                    </Button>
                </Link>
                <h1 className="text-4xl">{user.name}</h1>
            </div>
            <div>
                <ChoreTabs userId={userId} />
            </div>
        </>
    )
}

const TabFormat = "w-full text-lg";

function ChoreTabs({ userId }: { userId: number }) {
    return (
        <div className="w-full">
            <Tabs defaultValue="Due" className="mt-3 w-full">
                <TabsList className="w-full grid grid-cols-3">
                    <TabsTrigger value="Due" className={TabFormat}>
                        Due
                    </TabsTrigger>
                    <TabsTrigger value="Upcoming" className={TabFormat}>
                        Upcoming
                    </TabsTrigger>
                    <TabsTrigger value="Completed" className={TabFormat}>
                        Completed
                    </TabsTrigger>
                </TabsList>

                <TabsContent value="Due">
                    <ScrollArea className="h-screen">
                        <DueChores userId={userId} />
                    </ScrollArea>
                </TabsContent>

                <TabsContent value="Upcoming">
                    <ScrollArea className="h-screen">
                        <UpcomingChores userId={userId} />
                    </ScrollArea>
                </TabsContent>

                <TabsContent value="Completed">
                    <ScrollArea className="h-screen">
                        <CompletedChores userId={userId}/>
                    </ScrollArea>
                </TabsContent>
            </Tabs>
        </div>
    )
}


// The thing that shows ALL the due chores
function DueChores({ userId }: { userId: number }) {
    const {data, isPending} = useListUserChoreOccurrences(userId, {
        filter: "Due"
    });

    if (isPending) {
        return <div>Loading due chores...</div>
    }

    const chores = data?.data.items ?? [];

    if (chores.length === 0) {
        return (
            <p className="mt-6 text-center text-muted-foreground">
                No chores due 🎉
            </p>
        );
    }

    return (
        <div className="mt-3">
            {chores?.map(choreOccurrence => (
                <DueChoreCard
                    key={choreOccurrence.id}
                    id={choreOccurrence.id}
                    title = {choreOccurrence.chore.title}
                    description = {choreOccurrence.chore.description}
                    dueAt={choreOccurrence.currentDueAt}

                />
            ))}
        </div>
    )
}

// The thing that repreesnts one due chore
function DueChoreCard({ id, title, description, dueAt }: {id: number, title: string, description? : string | null, dueAt: string}) {
    const userId = useCurrentUserId();
    // We need access to the query client (the thing that makes the requests)
    const queryClient = useQueryClient();
    // We need to make the query for snoozing the chore
    const { mutateAsync: snoozeChore, isPending: isSnoozing, } = useSnoozeChore();
    const { mutateAsync: completeChore, isPending: isCompleting, } = useCompleteChore();


    async function handleSnoozeChore() {
        await snoozeChore({
            data: { userId },
            choreOccurrenceId: id,
        });
        await queryClient.invalidateQueries(
            {queryKey: getListUserChoreOccurrencesQueryKey(userId)}
        );
    }

    async function handleCompleteChore() {
        await completeChore({
            data: { userId },
            choreOccurrenceId: id,
        });
        await queryClient.invalidateQueries(
            {queryKey: getListUserChoreOccurrencesQueryKey(userId)}
        );
    }


    // Basically whenever we delete a chore or snooze it or whatever it would change the state of due and upcoming
    // So we may as well just refetch the whole thing, makes sense?
    // Not 100% efficient but its a lot easier than trying to manage the state on the frontend.

    return (
        <Card className="flex flex-col justify-between mb-3">
            <CardHeader>
                <CardTitle>{title}</CardTitle>
                {description && <CardDescription>{description}</CardDescription>}
            </CardHeader>
            <CardContent>
                <p>Due: {formatRelativeDate(dueAt)}</p>
            </CardContent>
            <CardFooter className="flex flex-row justify-center gap-10">
                <Button disabled={isSnoozing}  onClick={() => handleSnoozeChore()} variant="secondary">
                    <ClockPlus />
                    Snooze
                </Button>

                <Button disabled={isCompleting}  onClick={() => handleCompleteChore()} className="bg-primary text-primary-foreground">
                    <Check />
                    Done
                </Button>
            </CardFooter>
        </Card>
    )
}

function UpcomingChores({ userId }: { userId: number }) {
    const {data, isPending} = useListUserChoreOccurrences(userId, {
        filter: "Upcoming"
    });

    if (isPending) {
        return <div>Loading upcoming chores...</div>
    }

    const chores = data?.data.items ?? [];
    if (chores.length === 0) {
        return (
            <p className="mt-6 text-center text-muted-foreground">
                No upcoming chores 🎉
            </p>
        );
    }

    return (
        <div>
            {chores?.map(choreOccurrence => (
                <UpcomingChoreCard
                    key={choreOccurrence.id}
                    title = {choreOccurrence.chore.title}
                    description = {choreOccurrence.chore.description}
                    dueAt={choreOccurrence.currentDueAt}

                />
            ))}
        </div>
    )
}

function UpcomingChoreCard({ title, description, dueAt }: {title: string, description? : string | null, dueAt: string}){
    return (
        <div>
            <Card className="mb-4">
                <CardHeader>
                    <CardTitle>{title}</CardTitle>
                    {description && <CardDescription>{description}</CardDescription>}
                </CardHeader>
                <CardContent>
                    <p>Due: {formatRelativeDate(dueAt)}</p>
            </CardContent>
            </Card>
        </div>
    )
}


function CompletedChores({ userId }: { userId: number }) {
    const {data, isPending} = useListUserChoreOccurrences(userId, {
        filter: "Completed"
    });

    if (isPending) {
        return <div>Loading completed chores...</div>
    }
    const chores = data?.data.items;

    return (
        <div>
            {chores?.map(choreOccurrence => (
                <CompletedChoreCard
                    key={choreOccurrence.id}
                    title = {choreOccurrence.chore.title}
                    description = {choreOccurrence.chore.description}
                    completedAt={choreOccurrence.completedAt!}
                />
            ))}
        </div>
    )
}

function CompletedChoreCard({ title, description, completedAt }: {title: string, description? : string | null, completedAt: string}){
    return (
        <div>
            <Card className="mb-4">
                <CardHeader>
                    <CardTitle>{title}</CardTitle>
                    {description && <CardDescription>{description}</CardDescription>}
                </CardHeader>
                <CardContent>
                    <p>Completed: {formatRelativeDate(completedAt)}</p>
            </CardContent>
            </Card>
        </div>
    )
}

export function formatRelativeDate(input: string | Date): string {
    const date = typeof input === "string" ? new Date(input) : input;
    const now = new Date();

    const diffMs = date.getTime() - now.getTime();
    const diffMinutes = Math.round(diffMs / (1000 * 60));
    const diffHours = Math.round(diffMs / (1000 * 60 * 60));
    const diffDays = Math.round(diffMs / (1000 * 60 * 60 * 24));

    const absDays = Math.abs(diffDays);

    // More than 5 days away, show date
    if (absDays > 5) {
        return date.toLocaleDateString(undefined, {
            year: "numeric",
            month: "short",
            day: "numeric",
        });
    }

    if (Math.abs(diffMinutes) < 60) {
        return diffMinutes >= 0
            ? `in ${diffMinutes} minute${diffMinutes === 1 ? "" : "s"}`
            : `${Math.abs(diffMinutes)} minute${Math.abs(diffMinutes) === 1 ? "" : "s"} ago`;
    }

    if (Math.abs(diffHours) < 24) {
        return diffHours >= 0
            ? `in ${diffHours} hour${diffHours === 1 ? "" : "s"}`
            : `${Math.abs(diffHours)} hour${Math.abs(diffHours) === 1 ? "" : "s"} ago`;
    }

    return diffDays >= 0
        ? `in ${diffDays} day${diffDays === 1 ? "" : "s"}`
        : `${Math.abs(diffDays)} day${Math.abs(diffDays) === 1 ? "" : "s"} ago`;
}


export default User;
