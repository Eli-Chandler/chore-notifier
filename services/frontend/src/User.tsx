import {Link, useParams} from "react-router";
import {
    Tabs,
    TabsContent,
    TabsList,
    TabsTrigger,
} from "@/components/ui/tabs"
import {ScrollArea} from "@/components/ui/scroll-area.tsx";
import {useGetUser} from "@/api/users/users.ts";
import {AlarmClockIcon, ArrowLeft} from "lucide-react";
import {
    Card,
    CardAction,
    CardContent,
    CardDescription,
    CardFooter,
    CardHeader,
    CardTitle,
} from "@/components/ui/card"
import {Button} from "@/components/ui/button.tsx";
import * as React from "react";

function User() {
    const { userId } = useParams<{ userId: string }>()
    const {data: userData, isPending} = useGetUser(parseInt(userId!));

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
                <ChoreTabs />
            </div>
        </>
    )
}

const TabFormat = "w-full text-lg";

function ChoreTabs() {
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
                        <DueChores />
                    </ScrollArea>
                </TabsContent>

                <TabsContent value="Upcoming">
                    <ScrollArea className="h-screen">
                        <UpcomingChores />
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


function DueChores() {
    return (
        <div className="mt-3">
            <Card>
                <CardHeader>
                    <CardTitle>Card Title</CardTitle>
                    <CardDescription>Card Description</CardDescription>
                    <CardAction>Card Action</CardAction>
                </CardHeader>
                <CardContent>
                    <p>Card Content</p>
                </CardContent>
                <CardFooter>
                    <p>Card Footer</p>
                </CardFooter>
            </Card>
        </div>
    )
}

function UpcomingChores() {
    return (
        <div>
            Upcoming Chores
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
