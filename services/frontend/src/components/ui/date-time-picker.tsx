import { useState } from "react"
import { ChevronLeft, ChevronRight, CalendarDays, Clock } from "lucide-react"

function cn(...classes: (string | boolean | undefined | null | 0)[]) {
    return classes.filter(Boolean).join(" ")
}

interface CalendarProps {
    selected: Date | null;
    onSelect: (date: Date) => void;
}

function Calendar({ selected, onSelect }: CalendarProps) {
    const [currentMonth, setCurrentMonth] = useState(selected || new Date())

    const year = currentMonth.getFullYear()
    const month = currentMonth.getMonth()

    const firstDay = new Date(year, month, 1)
    const lastDay = new Date(year, month + 1, 0)
    const startingDay = firstDay.getDay()
    const totalDays = lastDay.getDate()

    const monthNames = [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    ]

    const days = []
    for (let i = 0; i < startingDay; i++) days.push(null)
    for (let i = 1; i <= totalDays; i++) days.push(i)

    const prevMonth = () => setCurrentMonth(new Date(year, month - 1, 1))
    const nextMonth = () => setCurrentMonth(new Date(year, month + 1, 1))

    const isSelected = (day: number | null) =>
        selected &&
        day &&
        selected.getDate() === day &&
        selected.getMonth() === month &&
        selected.getFullYear() === year

    const isToday = (day: number | null) => {
        const today = new Date()
        return (
            today.getDate() === day &&
            today.getMonth() === month &&
            today.getFullYear() === year
        )
    }

    return (
        <div className="p-3">
            <div className="flex items-center justify-between mb-4">
                <button
                    onClick={prevMonth}
                    className="p-1 rounded-md hover:bg-muted transition-colors"
                >
                    <ChevronLeft className="h-4 w-4" />
                </button>

                <span className="text-sm font-medium text-foreground">
                    {monthNames[month]} {year}
                </span>

                <button
                    onClick={nextMonth}
                    className="p-1 rounded-md hover:bg-muted transition-colors"
                >
                    <ChevronRight className="h-4 w-4" />
                </button>
            </div>

            <div className="grid grid-cols-7 gap-1 mb-2">
                {["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"].map((d) => (
                    <div
                        key={d}
                        className="text-center text-xs text-muted-foreground font-medium py-1"
                    >
                        {d}
                    </div>
                ))}
            </div>

            <div className="grid grid-cols-7 gap-1">
                {days.map((day, i) => (
                    <button
                        key={i}
                        onClick={() => day && onSelect(new Date(year, month, day))}
                        disabled={!day}
                        className={cn(
                            "h-8 w-8 rounded-md text-sm transition-colors",
                            !day && "invisible",
                            day && "hover:bg-muted",
                            isSelected(day) &&
                            "bg-primary text-primary-foreground hover:bg-primary",
                            isToday(day) &&
                            !isSelected(day) &&
                            "bg-muted font-semibold"
                        )}
                    >
                        {day}
                    </button>
                ))}
            </div>
        </div>
    )
}

interface TimeSelectProps {
    value: number;
    onChange: (value: number) => void;
    max: number;
}

function TimeSelect({ value, onChange, max }: TimeSelectProps) {
    return (
        <select
            value={value}
            onChange={(e) => onChange(parseInt(e.target.value))}
            className="h-9 w-[70px] rounded-md border bg-background px-2 text-sm
                       focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-1"
        >
            {Array.from({ length: max }, (_, i) => (
                <option key={i} value={i}>
                    {i.toString().padStart(2, "0")}
                </option>
            ))}
        </select>
    )
}

interface DateTimePickerProps {
    value?: Date | null;
    onChange?: (date: Date) => void;
    placeholder?: string;
}

export function DateTimePicker({
                                   value,
                                   onChange,
                                   placeholder = "Pick a date and time",
                               }: DateTimePickerProps) {
    const [internalDate, setInternalDate] = useState<Date | null>(null)
    const [hours, setHours] = useState(value?.getHours() ?? 12)
    const [minutes, setMinutes] = useState(value?.getMinutes() ?? 0)
    const [open, setOpen] = useState(false)

    const date = value ?? internalDate
    const setDate = onChange ?? setInternalDate

    const handleDateSelect = (selectedDate: Date) => {
        const newDate = new Date(selectedDate)
        newDate.setHours(hours, minutes, 0, 0)
        setDate(newDate)
    }

    const handleHoursChange = (newHours: number) => {
        setHours(newHours)
        if (date) {
            const newDate = new Date(date)
            newDate.setHours(newHours, minutes, 0, 0)
            setDate(newDate)
        }
    }

    const handleMinutesChange = (newMinutes: number) => {
        setMinutes(newMinutes)
        if (date) {
            const newDate = new Date(date)
            newDate.setHours(hours, newMinutes, 0, 0)
            setDate(newDate)
        }
    }

    const formatDisplay = () => {
        if (!date) return placeholder
        const dateStr = date.toLocaleDateString("en-US", {
            month: "short",
            day: "numeric",
            year: "numeric",
        })
        const h = hours % 12 || 12
        const ampm = hours < 12 ? "AM" : "PM"
        const m = minutes.toString().padStart(2, "0")
        return `${dateStr} at ${h}:${m} ${ampm}`
    }

    return (
        <div className="relative">
            <button
                type="button"
                onClick={() => setOpen(!open)}
                className={cn(
                    "w-full flex items-center h-10 px-3 rounded-md border bg-background text-left text-sm",
                    "hover:border-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2",
                    !date && "text-muted-foreground"
                )}
            >
                <CalendarDays className="mr-2 h-4 w-4 text-muted-foreground" />
                {formatDisplay()}
            </button>

            {open && (
                <>
                    <div
                        className="fixed inset-0 z-40"
                        onClick={() => setOpen(false)}
                    />

                    <div className="absolute left-0 top-full mt-2 z-50 rounded-md border bg-background shadow-md">
                        <Calendar selected={date} onSelect={handleDateSelect} />

                        <div className="border-t px-3 py-3">
                            <div className="flex items-center gap-2 mb-2">
                                <Clock className="h-4 w-4 text-muted-foreground" />
                                <span className="text-sm font-medium text-foreground">
                                    Time
                                </span>
                            </div>

                            <div className="flex items-center gap-2">
                                <TimeSelect value={hours} onChange={handleHoursChange} max={24} />
                                <span className="text-muted-foreground font-medium">:</span>
                                <TimeSelect value={minutes} onChange={handleMinutesChange} max={60} />
                                <span className="ml-2 text-sm text-muted-foreground">
                                    {hours < 12 ? "AM" : "PM"}
                                </span>
                            </div>
                        </div>

                        <div className="border-t p-3">
                            <button
                                type="button"
                                onClick={() => setOpen(false)}
                                className="w-full h-9 rounded-md bg-primary text-primary-foreground text-sm font-medium hover:bg-primary transition-colors"
                            >
                                Done
                            </button>
                        </div>
                    </div>
                </>
            )}
        </div>
    )
}

export default DateTimePicker
