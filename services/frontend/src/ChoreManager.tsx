import {Link} from "react-router";
import {Button} from "@/components/ui/button.tsx";
import {ArrowLeft, Pencil, PlusIcon} from "lucide-react";
import {
    Dialog,
    DialogClose,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {Textarea} from "@/components/ui/textarea.tsx";
import {
    Popover,
    PopoverContent,
    PopoverTrigger,
} from "@/components/ui/popover"
import * as React from "react"
import { ChevronDownIcon } from "lucide-react"
import { Calendar } from "@/components/ui/calendar"
import {
    DropdownMenu,
    DropdownMenuCheckboxItem,
    DropdownMenuContent,
    DropdownMenuLabel,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
    Card,
    CardAction,
    CardDescription,
    CardHeader,
    CardTitle,
} from "@/components/ui/card"

function ChoreManager() {
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
                <ChoreCard />
                <AddChore />
            </div>
        </>
    )
}

function AddChore() {
    const [startDate, setStartDate] = React.useState<Date | undefined>()
    const [endDate, setEndDate] = React.useState<Date | undefined>()
    return (
        <Dialog>
            <form>
                <DialogTrigger asChild>
                    <Button className="fixed bottom-6 right-6 rounded-full size-16 bg-primary text-primary-foreground shadow-lg">
                        <PlusIcon className="size-8"/>
                    </Button>
                </DialogTrigger>
                <DialogContent className="sm:max-w-[425px]">
                    <DialogHeader>
                        <DialogTitle>New Chore</DialogTitle>
                        <DialogDescription>
                            Enter the chore details below and click create.
                        </DialogDescription>
                    </DialogHeader>
                    <div className="grid gap-4">
                        <div className="grid gap-3">
                            <Label htmlFor="name-1">Name</Label>
                            <Input id="name-1" name="name" defaultValue="E.g. Empty Recycling" />
                        </div>
                        <div className="grid gap-3">
                            <Label htmlFor="description-1">Description</Label>
                            <Textarea
                                id="description-1"
                                name="description"
                                placeholder="E.g. Empty both the recycling bin and box to the communal bins outside."
                                className="min-h-32 resize-none"
                            />
                        </div>
                        <div className="flex flex-col gap-4">
                            <DatePicker label="Start" date={startDate} setDate={setStartDate} />
                            <div>
                                <DatePicker label="End" date={endDate} setDate={setEndDate} />
                            </div>
                        </div>
                        <div className="grid gap-3">
                            <Label htmlFor="interval-1">Intervals (Days)</Label>
                            <Input id="interval-1" name="intervals" defaultValue="E.g. 2" />
                        </div>
                        <div>
                            <Assignment />
                        </div>
                    </div>
                    <DialogFooter>
                        <DialogClose asChild>
                            <Button variant="outline">Cancel</Button>
                        </DialogClose>
                        <Button type="submit">Create</Button>
                    </DialogFooter>
                </DialogContent>
            </form>
        </Dialog>
    )
}

function DatePicker({ label, date, setDate }: { label: string, date: Date | undefined, setDate: React.Dispatch<React.SetStateAction<Date | undefined>> }) {
    const [open, setOpen] = React.useState(false)

    return (
        <div className="flex flex-col gap-3">
            <Label htmlFor={label.toLowerCase()} className="px-1">
                {label}
            </Label>
            <Popover open={open} onOpenChange={setOpen}>
                <PopoverTrigger asChild>
                    <Button
                        variant="outline"
                        id={label.toLowerCase()}
                        className="w-full justify-between font-normal"
                    >
                        {date ? date.toLocaleDateString() : "Select date"}
                        <ChevronDownIcon />
                    </Button>
                </PopoverTrigger>
                <PopoverContent className="w-auto overflow-hidden p-0" align="start">
                    <Calendar
                        mode="single"
                        selected={date}
                        captionLayout="dropdown"
                        onSelect={(d) => {
                            setDate(d)
                            setOpen(false)
                        }}
                    />
                </PopoverContent>
            </Popover>
        </div>
    )
}

function Assignment(){
    // List of users (what you would get from the API)
    const users = ["Joy", "Eli", "Yana", "Chase"];

    // Checked Map (Dictionary mapping username -> checked or not (true/false))
    const [checkedMap, setCheckedMap] = React.useState<Record<string, boolean>>({});

    // Set checked, looks at the pervious state of the checkedMap, and updates it with the new value for the given user
    function setChecked(user: string, value: boolean) {
        setCheckedMap((prev) => ({
            ...prev,
            [user]: value,
        }));
    }

    // Just utility to check In dictionary? Get dictionary value, else false
    function getChecked(user: string): boolean {
        return checkedMap[user] || false;
    }

    return (
        <DropdownMenu>
            <div className="mb-3">
                <Label htmlFor="assignment-1">Assign to</Label>
            </div>
            <DropdownMenuTrigger asChild>
                <Button
                    variant="outline"
                    className="w-full justify-between">
                    Select
                    <ChevronDownIcon />
                </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent className="w-56">
                <DropdownMenuLabel>Tenants</DropdownMenuLabel>
                <DropdownMenuSeparator />
                {/* for each user in the list of users, make a compolnent like this: */}
                {
                    users.map((user) => (
                        <DropdownMenuCheckboxItem
                            key={user}
                            checked={getChecked(user)}
                            onCheckedChange={(value) => setChecked(user, value as boolean)}
                        >
                            {user}
                        </DropdownMenuCheckboxItem>
                    ))
                }
            </DropdownMenuContent>
        </DropdownMenu>
    )
}

function ChoreCard() {
    return (
        <Card>
            <CardHeader className="flex flex-row gap-3">
                <div className="text-left">
                    <CardTitle>Empty Recycling</CardTitle>
                    <CardDescription>
                        Empty both the recycling bin and box to the communal bins outside.
                    </CardDescription>
                </div>
                <CardAction>
                    <Button variant="outline">
                        <Pencil />
                    </Button>
                </CardAction>
            </CardHeader>
        </Card>
    )
}


export default ChoreManager;