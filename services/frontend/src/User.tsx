import {Link, useParams} from "react-router";
import {
    Tabs,
    TabsContent,
    TabsList,
    TabsTrigger,
} from "@/components/ui/tabs"
import {ScrollArea} from "@/components/ui/scroll-area.tsx";
import {useGetUser, useListUserChoreOccurrences} from "@/api/users/users.ts";
import {AlarmClockIcon, ArrowLeft} from "lucide-react";
import {
    Card,
    CardContent,
    CardDescription,
    CardFooter,
    CardHeader,
    CardTitle,
} from "@/components/ui/card"
import {Button} from "@/components/ui/button.tsx";

function User() {
    const { userId } = useParams<{ userId: string }>();
    const actualUserId = parseInt(userId!)
    const {data: userData, isPending} = useGetUser(actualUserId);

    const user = userData?.data;

    if (isPending) {
        return <div>Loading...</div>
    }

    if (!user) {
        return <div>User not found</div>
    }

    return (
        <>
            <div className="flex flex-row gap-6">
                <Link to="/">
                    <Button className="bg-secondary text-primary">
                        <ArrowLeft className="left-6"/>
                        Back
                    </Button>
                </Link>
                <h1 className="text-4xl">{user.name}</h1>
            </div>
            <div>
                <ChoreTabs userId={actualUserId!} />
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
                        <CompletedChores />
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

    const chores = data?.data.items;

    return (
        <div className="mt-3">
            {chores?.map(choreOccurrence => (
                <DueChoreCard
                    key={choreOccurrence.id}
                    title = {choreOccurrence.chore.title}
                    description = {choreOccurrence.chore.description}
                    dueAt={choreOccurrence.currentDueAt}

                />
            ))}
        </div>
    )
}

// The thing that repreesnts one due chore
function DueChoreCard({ title, description, dueAt }: {title: string, description? : string | null, dueAt: string}) {
    return (
        <Card className="mb-4">
            <CardHeader>
                <CardTitle>{title}</CardTitle>
                {description && <CardDescription>{description}</CardDescription>}
            </CardHeader>
            <CardContent>
                <p>Due at: {new Date(dueAt).toLocaleString()}</p>
            </CardContent>
            <CardFooter>
                <Button variant="default">
                    <AlarmClockIcon className="mr-2" />
                    Mark as Complete
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

    const chores = data?.data.items;

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
                    <p>Due at: {new Date(dueAt).toLocaleString()}</p>
                </CardContent>
            </Card>
        </div>
    )
}


function CompletedChores() {
    return (
        <div>
            Completed Chores
        </div>
    )
}

export default User;
