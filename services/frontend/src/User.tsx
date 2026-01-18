import {Link, useParams} from "react-router";
import {
    Tabs,
    TabsContent,
    TabsList,
    TabsTrigger,
} from "@/components/ui/tabs"
import {ScrollArea} from "@/components/ui/scroll-area.tsx";
import {getListUserChoreOccurrencesQueryKey, useGetUser, useListUserChoreOccurrences} from "@/api/users/users.ts";
import {useGetUserStatistics} from "@/api/statistics/statistics.ts";
import {ArrowBigLeft, BellIcon, Check, ClockPlus} from "lucide-react";
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
import {Dialog, DialogHeader, DialogContent, DialogTitle, DialogTrigger} from "@/components/ui/dialog.tsx";
import {
    getGetNotificationPreferenceQueryKey, getListNotificationHistoryQueryKey,
    useAddNotificationPreference,
    useGetNotificationPreference, useListNotificationHistory
} from "@/api/notifications/notifications.ts";
import {Input} from "@/components/ui/input.tsx";
import {useState} from "react";
import {Label} from "@/components/ui/label.tsx";
import {Skeleton} from "@/components/ui/skeleton.tsx";
import type {DeliveryStatus} from "@/api/choreNotifierV1.schemas.ts";
import {Badge} from "@/components/ui/badge.tsx";

function useCurrentUserId() {
    const {userId} = useParams<{ userId: string }>();
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
            <div className="flex flex-row justify-between">
                <Link to="/">
                    <Button size="icon-lg" variant="secondary">
                        <ArrowBigLeft className="left-6"/>
                    </Button>
                </Link>
                <h1 className="text-5xl text-primary font-bitcount">{user.name}</h1>
                <NotificationPreference/>
            </div>
            <div>
                <ChoreTabs userId={userId}/>
            </div>
        </>
    )
}

function NotificationPreference() {
    const userId = useCurrentUserId();
    const {data} = useGetNotificationPreference(userId);
    const {data: notificationData, isPending: notificationsPending} = useListNotificationHistory(userId, {
        pageSize: 5
    });
    const {mutateAsync, isPending: isSaving} = useAddNotificationPreference();
    const queryClient = useQueryClient();

    const notifications = notificationData?.data.items ?? [];

    const savedTopicName = data?.data.type === "Ntfy" ? data.data.topicName : "";

    // Local state only tracks unsaved edits
    const [draft, setDraft] = useState<string | null>(null);

    // What to show in the input: draft if editing, otherwise saved value
    const channelId = draft ?? savedTopicName;
    const isChanged = draft !== null && draft !== savedTopicName;

    async function handleSave() {
        await mutateAsync({
            userId,
            data: {
                type: "Ntfy",
                topicName: channelId,
            },
        });
        setDraft(null);
        await queryClient.invalidateQueries({
            queryKey: getGetNotificationPreferenceQueryKey(userId)
        });
        await queryClient.invalidateQueries({
            queryKey: getListNotificationHistoryQueryKey(userId)
        })
    }

    return (
        <Dialog onOpenChange={() => setDraft(null)}> {/* Reset draft when closing */}
            <DialogTrigger asChild>
                <Button size="icon"><BellIcon/></Button>
            </DialogTrigger>

            <DialogContent>
                <DialogHeader>
                    <DialogTitle>Update Notification Preference</DialogTitle>
                </DialogHeader>
                <Label>
                    Ntfy Notification Channel
                </Label>
                <Input
                    value={channelId}
                    onChange={(e) => setDraft(e.target.value)}
                />

                <Button
                    onClick={handleSave}
                    disabled={isSaving || !channelId || !isChanged}
                >
                    Save
                </Button>
                <ScrollArea className="h-60">
                    {notificationsPending ? <Skeleton className="w-full h-40"/> :
                        <div className="flex flex-col gap-1">
                            {
                            notifications.map((notification) => (
                            <NotificationCard
                                key={notification.id}
                                title={notification.title}
                                message={notification.message}
                                attemptedAt={notification.attemptedAt}
                                deliveryStatus={notification.deliveryStatus}
                                failureReason={notification.failureReason}
                            />
                            ))
                            }
                        </div>
                    }

                </ScrollArea>
            </DialogContent>
        </Dialog>
    );
}

interface NotificationCardProps {
    title: string;
    message: string;
    attemptedAt: string;
    deliveryStatus: DeliveryStatus;
    failureReason?: string | null;
}

function NotificationCard({title, message, attemptedAt, deliveryStatus, failureReason}: NotificationCardProps) {
    const deliveryStatusColourMap: Record<DeliveryStatus, string> = {
        "Pending": "bg-gray-200",
        "Delivered": "bg-primary text-white",
        "Failed": "bg-destructive text-white",
    }

    return (
        <Card className="w-full">
            <CardHeader>
                <div className="flex justify-between items-center">
                    <Badge className={deliveryStatusColourMap[deliveryStatus]}>{deliveryStatus}</Badge>
                    <p className="text-xs">{formatRelativeDate(attemptedAt)}</p>
                </div>

                <CardTitle className="text-sm">{title}</CardTitle>
                <CardDescription>{message}</CardDescription>
                {failureReason && <p className="text-destructive"></p> }
            </CardHeader>
        </Card>
    );

}

const TabFormat = "w-full text-sm flex justify-center";

function ChoreTabs({userId}: { userId: number }) {
    return (
        <div className="w-full">
            <Tabs defaultValue="Due" className="mt-3 w-full">
                <TabsList className="w-full grid grid-cols-4">
                    <TabsTrigger value="Due" className={TabFormat}>
                        Due
                    </TabsTrigger>
                    <TabsTrigger value="Upcoming" className={TabFormat}>
                        Upcoming
                    </TabsTrigger>
                    <TabsTrigger value="Completed" className={TabFormat}>
                        Completed
                    </TabsTrigger>
                    <TabsTrigger value="Stats" className={TabFormat}>
                        Stats
                    </TabsTrigger>
                </TabsList>

                <TabsContent value="Due">
                    <ScrollArea className="h-screen">
                        <DueChores userId={userId}/>
                    </ScrollArea>
                </TabsContent>

                <TabsContent value="Upcoming">
                    <ScrollArea className="h-screen">
                        <UpcomingChores userId={userId}/>
                    </ScrollArea>
                </TabsContent>

                <TabsContent value="Completed">
                    <ScrollArea className="h-screen">
                        <CompletedChores userId={userId}/>
                    </ScrollArea>
                </TabsContent>

                <TabsContent value="Stats">
                    <UserStats userId={userId}/>
                </TabsContent>
            </Tabs>
        </div>
    )
}


// The thing that shows ALL the due chores
function DueChores({userId}: { userId: number }) {
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
                    title={choreOccurrence.chore.title}
                    description={choreOccurrence.chore.description}
                    dueAt={choreOccurrence.currentDueAt}

                />
            ))}
        </div>
    )
}

// The thing that repreesnts one due chore
function DueChoreCard({id, title, description, dueAt}: { id: number, title: string, description?: string | null, dueAt: string }) {
    const userId = useCurrentUserId();
    // We need access to the query client (the thing that makes the requests)
    const queryClient = useQueryClient();
    // We need to make the query for snoozing the chore
    const {mutateAsync: snoozeChore, isPending: isSnoozing,} = useSnoozeChore();
    const {mutateAsync: completeChore, isPending: isCompleting,} = useCompleteChore();


    async function handleSnoozeChore() {
        await snoozeChore({
            data: {userId},
            choreOccurrenceId: id,
        });
        await queryClient.invalidateQueries(
            {queryKey: getListUserChoreOccurrencesQueryKey(userId)}
        );
    }

    async function handleCompleteChore() {
        await completeChore({
            data: {userId},
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
                <Button disabled={isSnoozing} onClick={() => handleSnoozeChore()} variant="secondary">
                    <ClockPlus/>
                    Snooze
                </Button>

                <Button disabled={isCompleting} onClick={() => handleCompleteChore()} className="bg-primary text-primary-foreground">
                    <Check/>
                    Done
                </Button>
            </CardFooter>
        </Card>
    )
}

function UpcomingChores({userId}: { userId: number }) {
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
                    title={choreOccurrence.chore.title}
                    description={choreOccurrence.chore.description}
                    dueAt={choreOccurrence.currentDueAt}

                />
            ))}
        </div>
    )
}


function UpcomingChoreCard({title, description, dueAt}: { title: string, description?: string | null, dueAt: string }) {
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


function CompletedChores({userId}: { userId: number }) {
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
                    title={choreOccurrence.chore.title}
                    description={choreOccurrence.chore.description}
                    completedAt={choreOccurrence.completedAt!}
                />
            ))}
        </div>
    )
}

function CompletedChoreCard({title, description, completedAt}: { title: string, description?: string | null, completedAt: string }) {
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

function formatTimeSpan(timeSpan: string): string {
    // Format: [-][d.]hh:mm:ss[.fffffff]
    const match = timeSpan.match(/^-?(?:(\d+)\.)?(\d{2}):(\d{2}):(\d{2})/);
    if (!match) return timeSpan;

    const days = match[1] ? parseInt(match[1]) : 0;
    const hours = parseInt(match[2]);
    const minutes = parseInt(match[3]);

    const parts: string[] = [];
    if (days > 0) parts.push(`${days}d`);
    if (hours > 0) parts.push(`${hours}h`);
    if (minutes > 0 || parts.length === 0) parts.push(`${minutes}m`);

    return parts.join(" ");
}

function UserStats({userId}: { userId: number }) {
    const {data, isPending} = useGetUserStatistics(userId);

    if (isPending) {
        return <div className="mt-6 text-center text-muted-foreground">Loading stats... 📊</div>;
    }

    const stats = data?.data;

    if (!stats) {
        return <div className="mt-6 text-center text-muted-foreground">No statistics available 😢</div>;
    }

    const completionRate = stats.totalChoresAssigned > 0
        ? (stats.totalChoresCompleted / stats.totalChoresAssigned) * 100
        : 0;

    return (
        <div className="mt-3 grid grid-cols-2 gap-4">
            <Card className="border-2 border-blue-200 bg-gradient-to-br from-blue-50 to-white">
                <CardHeader className="pb-2">
                    <CardTitle className="text-lg flex items-center gap-2">
                        📋 Assigned
                    </CardTitle>
                </CardHeader>
                <CardContent>
                    <p className="text-4xl font-bold text-blue-600">{stats.totalChoresAssigned}</p>
                    <p className="text-sm text-muted-foreground">total chores</p>
                </CardContent>
            </Card>

            <Card className="border-2 border-green-200 bg-gradient-to-br from-green-50 to-white">
                <CardHeader className="pb-2">
                    <CardTitle className="text-lg flex items-center gap-2">
                        ✅ Completed
                    </CardTitle>
                </CardHeader>
                <CardContent>
                    <p className="text-4xl font-bold text-green-600">{stats.totalChoresCompleted}</p>
                    <p className="text-sm text-muted-foreground">
                        {completionRate.toFixed(0)}% completion rate 🎯
                    </p>
                </CardContent>
            </Card>

            <Card className="border-2 border-amber-200 bg-gradient-to-br from-amber-50 to-white">
                <CardHeader className="pb-2">
                    <CardTitle className="text-lg flex items-center gap-2">
                        😴 Snooze Rate
                    </CardTitle>
                </CardHeader>
                <CardContent>
                    <p className="text-4xl font-bold text-amber-600">{(stats.snoozeFrequency * 100).toFixed(0)}%</p>
                    <p className="text-sm text-muted-foreground">
                        {stats.snoozeFrequency < 0.2 ? "Great discipline! 💪" :
                         stats.snoozeFrequency < 0.5 ? "Not bad! 👍" : "Snooze master 😅"}
                    </p>
                </CardContent>
            </Card>

            <Card className="border-2 border-purple-200 bg-gradient-to-br from-purple-50 to-white">
                <CardHeader className="pb-2">
                    <CardTitle className="text-lg flex items-center gap-2">
                        ⏱️ Avg Time
                    </CardTitle>
                </CardHeader>
                <CardContent>
                    <p className="text-4xl font-bold text-purple-600">{formatTimeSpan(stats.averageCompletionTime)}</p>
                    <p className="text-sm text-muted-foreground">to complete chores 🚀</p>
                </CardContent>
            </Card>
        </div>
    );
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

    if (Math.abs(diffMinutes) < 1) {
        return "now";
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
