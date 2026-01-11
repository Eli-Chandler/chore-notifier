import {Link} from "react-router";
import {Button} from "@/components/ui/button.tsx";
import {ArrowLeft, BellIcon, BrushCleaning, Pencil, UserIcon, XIcon} from "lucide-react";
import {
    Dialog,
    DialogClose,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from "@/components/ui/dialog";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Textarea} from "@/components/ui/textarea.tsx";
import {
    Popover,
    PopoverContent,
    PopoverTrigger,
} from "@/components/ui/popover";
import * as React from "react";
import {ChevronDownIcon} from "lucide-react";
import {Calendar} from "@/components/ui/calendar";
import {
    DropdownMenu,
    DropdownMenuCheckboxItem,
    DropdownMenuContent, DropdownMenuItem,
    DropdownMenuLabel,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
    Card,
    CardAction,
    CardDescription,
    CardHeader,
    CardTitle,
} from "@/components/ui/card";
import {
    getListChoresInfiniteQueryKey,
    getListChoresQueryKey,
    useCreateChore,
    useListChoresInfinite
} from "@/api/chores/chores.ts";
import {useListUsersInfinite} from "@/api/users/users.ts";
import {useQueryClient} from "@tanstack/react-query";
import {useState} from "react";

function ChoreManager() {
    const queryClient = useQueryClient();
    const {data: choresData, fetchNextPage, hasNextPage, isPending: choresPending} = useListChoresInfinite(
        undefined,
        {query: {getNextPageParam: (lastPage) => lastPage.data.nextCursor}}
    );

    const {data: usersData, isPending: usersPending} = useListUsersInfinite(
        undefined,
        {
            query: {
                getNextPageParam: (lastPage) => lastPage.data.nextCursor ?? undefined,
            }
        }
    );
    const chores = choresData?.pages.flatMap(x => x.data.items) ?? [];
    const users = usersData?.pages.flatMap(x => x.data.items) ?? [];

    return (
        <>
            <div className="flex flex-row gap-6">
                <Link to="/">
                    <Button className="bg-secondary text-primary">
                        <ArrowLeft className="left-6"/>
                        Back
                    </Button>
                </Link>
                <h1 className="text-4xl">Chores</h1>
            </div>
            <div className="mt-6">
                {choresPending && <p>Loading Chores...</p>}
                {chores.map((chore) => (
                    <div key={chore.id} className="mb-4">
                        <ChoreCard title={chore.title} description={chore.description ?? null}/>
                    </div>
                ))}
                {!usersPending && <AddChore availableUsers={users}/>}
            </div>
        </>
    );
}

type AddChoreProps = {
    availableUsers: { id: number; name: string }[];
};

function AddChore({availableUsers}: AddChoreProps) {
    const [title, setTitle] = React.useState("");
    const [description, setDescription] = React.useState("");
    const [intervalDays, setIntervalDays] = React.useState<number | "">("");
    const [startDate, setStartDate] = React.useState<Date | undefined>();
    const [endDate, setEndDate] = React.useState<Date | undefined>();
    const [assignees, setAssignees] = React.useState<{ id: number, name: string }[]>([]);

    const [isDialogOpen, setIsDialogOpen] = useState<boolean>(false);

    const queryClient = useQueryClient();
    const {mutateAsync: createChore, isPending} = useCreateChore();

    async function handleSubmit(e: React.FormEvent) {
        console.log("Clicked!")
        e.preventDefault();
        if (!title || !intervalDays || !startDate) return;

        await createChore({
            data: {
                title,
                description,
                choreSchedule: {
                    start: startDate.toISOString(),
                    intervalDays: Number(intervalDays),
                    until: endDate ? endDate.toISOString() : null,
                },
                snoozeDuration: "1",
                assigneeUserIds: assignees.map(a => a.id),
            },
        });
        await queryClient.invalidateQueries({queryKey: getListChoresInfiniteQueryKey()});
        setIsDialogOpen(false);
        // Reset form
        setTitle("");
        setDescription("");
        setIntervalDays("");
        setStartDate(undefined);
        setEndDate(undefined);
        setAssignees([]);
    }

    return (
        <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
            <DialogTrigger asChild>
                <Button
                    className="fixed bottom-6 right-6 rounded-full size-16 bg-primary text-primary-foreground shadow-lg">
                    <BrushCleaning className="size-8"/>
                </Button>
            </DialogTrigger>

            <DialogContent className="sm:max-w-[425px]">
                <form onSubmit={handleSubmit}>
                    <DialogHeader>
                        <DialogTitle>New Chore</DialogTitle>
                        <DialogDescription>
                            Enter the chore details below and click create.
                        </DialogDescription>
                    </DialogHeader>
                    <div className="grid gap-4">
                        <div className="grid gap-3">
                            <Label htmlFor="name-1">Name</Label>
                            <Input
                                id="name-1"
                                value={title}
                                onChange={(e) => setTitle(e.target.value)}
                                placeholder="E.g. Empty Recycling"
                            />
                        </div>
                        <div className="grid gap-3">
                            <Label htmlFor="description-1">Description</Label>
                            <Textarea
                                id="description-1"
                                value={description}
                                onChange={(e) => setDescription(e.target.value)}
                                placeholder="E.g. Empty both the recycling bin and box to the communal bins outside."
                                className="min-h-32 resize-none"
                            />
                        </div>
                        <div className="flex flex-col gap-4">
                            <DatePicker label="Start" date={startDate} setDate={setStartDate}/>
                            <DatePicker label="End" date={endDate} setDate={setEndDate}/>
                        </div>
                        <div className="grid gap-3">
                            <Label htmlFor="interval-1">Intervals (Days)</Label>
                            <Input
                                id="interval-1" type="number"
                                value={intervalDays} onChange={(e) => setIntervalDays(Number(e.target.value))}
                                placeholder="E.g. 2"
                            />
                        </div>
                        <Assignment
                            availableUsers={availableUsers}
                            selectedUsers={assignees}
                            onSelectedUsersChange={setAssignees}
                        />
                    </div>
                    <DialogFooter className="mt-10">
                        <DialogClose asChild>
                            <Button type="button" variant="outline">Cancel</Button>
                        </DialogClose>
                        <Button type="submit">Create</Button>
                    </DialogFooter>
                </form>
            </DialogContent>

        </Dialog>
    );
}

function DatePicker({label, date, setDate}: {
    label: string;
    date?: Date;
    setDate: React.Dispatch<React.SetStateAction<Date | undefined>>
}) {
    const [open, setOpen] = React.useState(false);

    return (
        <div className="flex flex-col gap-3">
            <Label htmlFor={label.toLowerCase()} className="px-1">{label}</Label>
            <Popover open={open} onOpenChange={setOpen}>
                <PopoverTrigger asChild>
                    <Button variant="outline" id={label.toLowerCase()} className="w-full justify-between font-normal">
                        {date ? date.toLocaleDateString() : "Select date"}
                        <ChevronDownIcon/>
                    </Button>
                </PopoverTrigger>
                <PopoverContent className="w-auto overflow-hidden p-0" align="start">
                    <Calendar mode="single" selected={date} captionLayout="dropdown" onSelect={(d) => {
                        setDate(d);
                        setOpen(false)
                    }}/>
                </PopoverContent>
            </Popover>
        </div>
    );
}

type AssignmentProps = {
    availableUsers: { id: number; name: string }[];
    selectedUsers: { id: number; name: string }[];
    onSelectedUsersChange: (selectedUsers: { id: number; name: string }[]) => void;
};

function Assignment({availableUsers, selectedUsers, onSelectedUsersChange}: AssignmentProps) {
    const [isOpen, setIsOpen] = useState<boolean>(false);

    function addUser(user: { id: number; name: string }) {
        onSelectedUsersChange([...selectedUsers, user]);
    }

    function removeUser(userId: number) {
        onSelectedUsersChange(selectedUsers.filter(u => u.id !== userId));
    }

    const unselectedUsers = availableUsers.filter(
        au => !selectedUsers.some(su => su.id === au.id)
    );

    return (
        <div className="flex flex-col gap-1">
            <Label className="mb-1">Assign to</Label>
            <div className="flex flex-wrap gap-2">
                {selectedUsers.map(user => (
                    <Button key={user.id} variant="secondary" size="sm" onClick={() => removeUser(user.id)}>
                        {user.name} <XIcon/>
                    </Button>
                ))}
            </div>
            <DropdownMenu open={isOpen} onOpenChange={setIsOpen}>
                <DropdownMenuTrigger asChild>
                    <Button variant="outline" className="w-full justify-between">
                        Select
                        <ChevronDownIcon/>
                    </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent className="w-56">
                    <DropdownMenuLabel>Tenants</DropdownMenuLabel>
                    <DropdownMenuSeparator/>
                    {unselectedUsers.map(user => (
                        <DropdownMenuItem key={user.id} onClick={() => addUser(user)}>
                            {user.name}
                        </DropdownMenuItem>
                    ))}
                </DropdownMenuContent>
            </DropdownMenu>
        </div>
    );
}

function ChoreCard({title, description}: { title: string; description: string | null }) {
    return (
        <Card>
            <CardHeader className="flex justify-between gap-3">
                <div className="text-left">
                    <CardTitle>{title}</CardTitle>
                    {description && <CardDescription>{description}</CardDescription>}

                </div>
                <div className="flex gap-2">
                    <CardAction>
                        <div className="flex gap-1 items-center">

                            <Button variant="default"> Eli<BellIcon/></Button>
                        </div>
                    </CardAction>
                    <CardAction>
                        <Button variant="secondary"><Pencil/></Button>
                    </CardAction>
                </div>
            </CardHeader>
        </Card>
    );
}

export default ChoreManager;
