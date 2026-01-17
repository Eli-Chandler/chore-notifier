import {Button} from "@/components/ui/button.tsx";
import {Link} from "react-router";
import {Trash2, UserPlus} from "lucide-react";
import {
    useDeleteUser,
    useCreateUser,
    useListUsersInfinite,
    getListUsersInfiniteQueryKey
} from "@/api/users/users.ts";
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
import {Popover, PopoverContent, PopoverTrigger} from "@/components/ui/popover.tsx";
import {Input} from "@/components/ui/input.tsx";
import {Label} from "@/components/ui/label.tsx";
import {useState} from "react";
import {useQueryClient} from "@tanstack/react-query";

const buttonFormat = "w-full h-12 text-xl flex items-center justify-center";

function ChoreManagement() {
    return (
        <Link
            to="/choreManagement">
            <Button variant="secondary" className={buttonFormat} >
                Chore Management
            </Button>
        </Link> ) }

function HomePage() {
    const queryClient = useQueryClient();
    const { data, isPending } = useListUsersInfinite(
        undefined,
        {
            query: {
                getNextPageParam: (lastPage) => lastPage.data.nextCursor ?? undefined,
            }
        }
    );
    const {mutateAsync: deleteUser} = useDeleteUser();
    const {mutateAsync: createUser} = useCreateUser();

    const users = data?.pages.flatMap(x => x.data.items);


    async function handleAddTenant(name: string) {
        await createUser({data: {name}});
        await queryClient.invalidateQueries({queryKey: getListUsersInfiniteQueryKey()});
    }

    // Handler to delete a tenant
    async function handleDeleteUser(id: number) {
        await deleteUser({userId: id});
        await queryClient.invalidateQueries({queryKey: getListUsersInfiniteQueryKey()});
    }

    if (isPending) return null;

    return (
        <>
            <ChoreManagement />
            <img
                src="/cleaningPig.gif"
                alt="Cleaning pig"
                className="mx-auto mb-4 mt-5 w-48"
            />
            <div className="flex flex-col gap-10 items-center h-screen">
                <h1 className="text-4xl text-primary font-bitcount">Who Are You?</h1>
                {isPending && <p>Loading tenants...</p>}
                {users && users.map((user) => (
                    <div key={user.id} className="flex w-full gap-2">
                        <Link to={`/${user.id}`} className="flex-1">
                            <Button variant="secondary" className={buttonFormat}>
                                {user.name}
                            </Button>
                        </Link>

                        <DeleteUserDialog
                            userName={user.name}
                            onConfirm={() => handleDeleteUser(user.id)}
                        />
                    </div>
                ))}

                <AddTenant onAddTenant={handleAddTenant}/>
            </div>
        </>
    );
}

type Props = {
    onAddTenant: (name: string) => Promise<void>;
};

function AddTenant({onAddTenant}: Props) {
    const [name, setName] = useState("");
    const [isOpen, setIsOpen] = useState(false);

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        if (!name) return;
        await onAddTenant(name);
        setName("");
        setIsOpen(false);
    }

    return (
        <Dialog open={isOpen} onOpenChange={setIsOpen}>
            <DialogTrigger asChild>
                <Button
                    className="fixed bottom-6 right-6 rounded-full size-16 bg-primary text-primary-foreground shadow-lg">
                    <UserPlus className="size-8"/>
                </Button>
            </DialogTrigger>

            <DialogContent className="sm:max-w-[425px]">
                <form onSubmit={handleSubmit}>
                    <DialogHeader>
                        <DialogTitle>New Tenant</DialogTitle>
                        <DialogDescription>
                            Enter name and save.
                        </DialogDescription>
                    </DialogHeader>

                    <div className="grid gap-4">
                        <div className="grid gap-3">
                            <Label htmlFor="name">Name</Label>
                            <Input
                                id="name"
                                value={name}
                                onChange={(e) => setName(e.target.value)}
                                required
                            />
                        </div>
                    </div>

                    <DialogFooter className="mt-10">
                        <DialogClose asChild>
                            <Button variant="outline">Cancel</Button>
                        </DialogClose>
                        <Button type="submit">Save</Button>
                    </DialogFooter>
                </form>
            </DialogContent>
        </Dialog>
    );
}

type DeleteUserDialogProps = {
    userName: string;
    onConfirm: () => Promise<void>;
};

function DeleteUserDialog({ userName, onConfirm }: DeleteUserDialogProps) {
    const [isOpen, setIsOpen] = useState(false);
    const [isPending, setIsPending] = useState(false);

    async function handleDelete() {
        setIsPending(true);
        await onConfirm();
        setIsPending(false);
        setIsOpen(false);
    }

    return (
        <Popover open={isOpen} onOpenChange={setIsOpen}>
            <PopoverTrigger asChild>
                <Button
                    size="icon-lg"
                    variant="destructive"
                    className="h-12 w-12 flex items-center justify-center"
                >
                    <Trash2 />
                </Button>
            </PopoverTrigger>
            <PopoverContent className="w-64">
                <div className="grid gap-4">
                    <p className="text-sm">
                        Remove <span className="font-semibold">{userName}</span>?
                    </p>
                    <div className="flex justify-end gap-2">
                        <Button variant="outline" size="sm" onClick={() => setIsOpen(false)}>
                            Cancel
                        </Button>
                        <Button variant="destructive" size="sm" onClick={handleDelete} disabled={isPending}>
                            {isPending ? "Removing..." : "Remove"}
                        </Button>
                    </div>
                </div>
            </PopoverContent>
        </Popover>
    );
}

export default HomePage;
