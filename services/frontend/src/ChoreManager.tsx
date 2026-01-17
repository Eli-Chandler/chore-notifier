import {Link} from "react-router";
import {Button} from "@/components/ui/button.tsx";
import {ArrowBigLeft, BrushCleaning, Pencil, UserPlus, XIcon} from "lucide-react";
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
import * as React from "react";
import {ChevronDownIcon} from "lucide-react";
import {DateTimePicker} from "@/components/ui/date-time-picker.tsx";
import {
    DropdownMenu,
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
    useAddChoreAssignee,
    useCreateChore,
    useListChoresInfinite,
    useRemoveChoreAssignee,
    useUpdateChore
} from "@/api/chores/chores.ts";
import {useListUsersInfinite} from "@/api/users/users.ts";
import {useQueryClient} from "@tanstack/react-query";
import {useEffect, useState} from "react";
import type {ListChoresResponseItem} from "@/api/choreNotifierV1.schemas.ts";

function ChoreManager() {
    const {data: choresData, isPending: choresPending} = useListChoresInfinite(
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
            <div className="flex flex-row items-center justify-between">
                <Link to="/">
                    <Button size="icon-lg" variant="secondary">
                        <ArrowBigLeft className="left-6"/>
                    </Button>
                </Link>
                <h1 className="text-5xl font-bitcount">Chores</h1>
                <div></div>
            </div>
            <div className="mt-6">
                {choresPending && <p>Loading Chores...</p>}
                {chores.map((chore) => (
                    <div key={chore.id} className="mb-4">
                        <ChoreCard chore={chore} availableUsers={users}/>
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
    const {mutateAsync: createChore} = useCreateChore();

    async function handleSubmit(e: React.FormEvent) {
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
                            Enter the chore details below and create.
                        </DialogDescription>
                    </DialogHeader>
                    <div className="grid gap-4 mt-3">
                        <div className="grid gap-3">
                            <Label className="font-bold" htmlFor="name-1">Name</Label>
                            <Input
                                id="name-1"
                                value={title}
                                onChange={(e) => setTitle(e.target.value)}
                                placeholder="E.g. Empty Recycling"
                            />
                        </div>
                        <div className="grid gap-3">
                            <Label className="font-bold" htmlFor="description-1">Description</Label>
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
                            <Label className="font-bold" htmlFor="interval-1">Intervals (Days)</Label>
                            <Input
                                id="interval-1"
                                type="number"
                                value={intervalDays}
                                onChange={(e) =>
                                    setIntervalDays(e.target.value === "" ? "" : Number(e.target.value))
                                }
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

interface UpdateChoreProps {
    chore: ListChoresResponseItem;
}

export function UpdateChore({chore}: UpdateChoreProps) {
    const [title, setTitle] = useState(chore.title);
    const [description, setDescription] = useState(chore.description ?? "");
    const [intervalDays, setIntervalDays] = useState<number | "">(chore.choreSchedule.intervalDays);
    const [startDate, setStartDate] = useState<Date | undefined>(new Date(chore.choreSchedule.start));
    const [endDate, setEndDate] = useState<Date | undefined>(
        chore.choreSchedule.until ? new Date(chore.choreSchedule.until) : undefined
    );

    const [isDialogOpen, setIsDialogOpen] = useState(false);

    const queryClient = useQueryClient();
    const {mutateAsync: updateChore, isPending} = useUpdateChore();

    // Reset form when chore prop changes or dialog opens
    useEffect(() => {
        if (isDialogOpen) {
            // eslint-disable-next-line react-hooks/set-state-in-effect
            setTitle(chore.title);
            setDescription(chore.description ?? "");
            setIntervalDays(chore.choreSchedule.intervalDays);
            setStartDate(new Date(chore.choreSchedule.start));
            setEndDate(chore.choreSchedule.until ? new Date(chore.choreSchedule.until) : undefined);
        }
    }, [isDialogOpen, chore]);

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        if (!title || !intervalDays || !startDate) return;

        await updateChore({
            choreId: chore.id,
            data: {
                title,
                description: description || null,
                choreSchedule: {
                    start: startDate.toISOString(),
                    intervalDays: Number(intervalDays),
                    until: endDate ? endDate.toISOString() : null,
                },
                snoozeDuration: chore.snoozeDuration,
            },
        });

        await queryClient.invalidateQueries({queryKey: getListChoresInfiniteQueryKey()});
        setIsDialogOpen(false);
    }

    return (
        <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
            <DialogTrigger asChild>
                <Button variant="secondary" size="icon">
                    <Pencil className="size-4"/>
                </Button>
            </DialogTrigger>

            <DialogContent className="sm:max-w-[425px]">
                <form onSubmit={handleSubmit}>
                    <DialogHeader>
                        <DialogTitle>Edit Chore</DialogTitle>
                        <DialogDescription>
                            Update the chore details below and save.
                        </DialogDescription>
                    </DialogHeader>
                    <div className="grid gap-4 mt-3">
                        <div className="grid gap-3">
                            <Label className="font-bold" htmlFor="name-edit">Name</Label>
                            <Input
                                id="name-edit"
                                value={title}
                                onChange={(e) => setTitle(e.target.value)}
                                placeholder="E.g. Empty Recycling"
                            />
                        </div>
                        <div className="grid gap-3">
                            <Label className="font-bold" htmlFor="description-edit">Description</Label>
                            <Textarea
                                id="description-edit"
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
                            <Label className="font-bold" htmlFor="interval-edit">Intervals (Days)</Label>
                            <Input
                                id="interval-edit"
                                type="number"
                                value={intervalDays}
                                onChange={(e) =>
                                    setIntervalDays(e.target.value === "" ? "" : Number(e.target.value))
                                }
                                placeholder="E.g. 2"
                            />
                        </div>
                    </div>
                    <DialogFooter className="mt-10">
                        <DialogClose asChild>
                            <Button type="button" variant="outline">Cancel</Button>
                        </DialogClose>
                        <Button type="submit" disabled={isPending}>
                            {isPending ? "Saving..." : "Save"}
                        </Button>
                    </DialogFooter>
                </form>
            </DialogContent>
        </Dialog>
    );
}

interface ManageAssigneesProps {
    choreId: number;
    initialAssignees: { id: number; name: string }[];
    availableUsers: { id: number; name: string }[];
}

function ManageAssignees({choreId, initialAssignees, availableUsers}: ManageAssigneesProps) {
    const [isDialogOpen, setIsDialogOpen] = useState(false);
    const [assignees, setAssignees] = useState(initialAssignees);

    // Sync with prop changes (e.g., after query invalidation)
    useEffect(() => {
        setAssignees(initialAssignees);
    }, [initialAssignees]);

    const queryClient = useQueryClient();
    const {mutateAsync: addAssignee, isPending: isAdding} = useAddChoreAssignee();
    const {mutateAsync: removeAssignee, isPending: isRemoving} = useRemoveChoreAssignee();

    const isPending = isAdding || isRemoving;

    async function handleAddUser(userId: number) {
        const result = await addAssignee({choreId, userId});
        setAssignees(result.data.assignees);
        await queryClient.invalidateQueries({queryKey: getListChoresInfiniteQueryKey()});
    }

    async function handleRemoveUser(userId: number) {
        await removeAssignee({choreId, userId});
        setAssignees(prev => prev.filter(a => a.id !== userId));
        await queryClient.invalidateQueries({queryKey: getListChoresInfiniteQueryKey()});
    }

    const unassignedUsers = availableUsers.filter(
        u => !assignees.some(a => a.id === u.id)
    );

    return (
        <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
            <DialogTrigger asChild>
                <Button variant="outline" size="icon">
                    <UserPlus className="size-4"/>
                </Button>
            </DialogTrigger>

            <DialogContent className="sm:max-w-[425px]">
                <DialogHeader>
                    <DialogTitle>Manage Assignees</DialogTitle>
                    <DialogDescription>
                        Add or remove users assigned to this chore.
                    </DialogDescription>
                </DialogHeader>
                <div className="grid gap-4 mt-3">
                    <div className="grid gap-3">
                        <Label className="font-bold">Current Assignees</Label>
                        {assignees.length === 0 ? (
                            <p className="text-sm text-muted-foreground">No assignees yet</p>
                        ) : (
                            <div className="flex flex-wrap gap-2">
                                {assignees.map(user => (
                                    <Button
                                        key={user.id}
                                        variant="secondary"
                                        size="sm"
                                        disabled={isPending}
                                        onClick={() => handleRemoveUser(user.id)}
                                    >
                                        {user.name} <XIcon/>
                                    </Button>
                                ))}
                            </div>
                        )}
                    </div>
                    {unassignedUsers.length > 0 && (
                        <div className="grid gap-3">
                            <Label className="font-bold">Add Assignee</Label>
                            <DropdownMenu>
                                <DropdownMenuTrigger asChild>
                                    <Button variant="outline" className="w-full justify-between" disabled={isPending}>
                                        Select user to add
                                        <ChevronDownIcon/>
                                    </Button>
                                </DropdownMenuTrigger>
                                <DropdownMenuContent className="w-56">
                                    <DropdownMenuLabel>Available Users</DropdownMenuLabel>
                                    <DropdownMenuSeparator/>
                                    {unassignedUsers.map(user => (
                                        <DropdownMenuItem key={user.id} onClick={() => handleAddUser(user.id)}>
                                            {user.name}
                                        </DropdownMenuItem>
                                    ))}
                                </DropdownMenuContent>
                            </DropdownMenu>
                        </div>
                    )}
                </div>
                <DialogFooter className="mt-6">
                    <DialogClose asChild>
                        <Button type="button">Done</Button>
                    </DialogClose>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}

function DatePicker({label, date, setDate,}: {
    label: string
    date?: Date
    setDate: React.Dispatch<React.SetStateAction<Date | undefined>>
}) {
    return (
        <div className="flex flex-col gap-3">
            <Label className="px-1 font-bold">{label}</Label>
            <DateTimePicker
                value={date}
                onChange={setDate}
                placeholder={`Pick ${label.toLowerCase()} date`}
            />
        </div>
    )
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
            <Label className="font-bold mb-1">Assign to</Label>
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

function ChoreCard({chore, availableUsers}: { chore: ListChoresResponseItem; availableUsers: { id: number; name: string }[] }) {
    return (
        <Card>
            <CardHeader className="flex justify-between gap-3">
                <div className="text-left">
                    <CardTitle>{chore.title}</CardTitle>
                    {chore.description && <CardDescription>{chore.description}</CardDescription>}
                </div>
                <div className="flex gap-2">
                    <CardAction>
                        <ManageAssignees choreId={chore.id} initialAssignees={chore.assignedUsers} availableUsers={availableUsers}/>
                    </CardAction>
                    <CardAction>
                        <UpdateChore chore={chore}/>
                    </CardAction>
                </div>
            </CardHeader>
        </Card>
    );
}

export default ChoreManager;
