import {Button} from "@/components/ui/button.tsx";
import {Link} from "react-router";
const buttonFormat = "w-full h-12 text-xl"

function CreateChore() {
    return (
        <Link to="/choreManagement">
            <Button
                variant="secondary"
                className={buttonFormat}
            >
                Chore Management
            </Button>
        </Link>
    )
}

function HomePage() {
    return (
        <>
            <CreateChore />

            <div className="flex flex-col gap-10 items-center justify-center h-screen">
                <h1 className="text-4xl text-primary">Who Are You?</h1>
                <h2 className="text-2xl text-secondary-foreground">Tenants:</h2>

                <Link to="/joy" className="w-full">
                    <Button variant="secondary" className={buttonFormat}>
                        Joy
                    </Button>
                </Link>

                <Link to="/eli" className="w-full">
                    <Button variant="secondary" className={buttonFormat}>
                        Eli
                    </Button>
                </Link>

                <Link to="/yana" className="w-full">
                    <Button variant="secondary"className={buttonFormat}>
                        Yana
                    </Button>
                </Link>

                <Link to="/chase" className="w-full">
                    <Button variant="secondary" className={buttonFormat}>
                        Chase
                    </Button>
                </Link>
            </div>
        </>
    )
}

export default HomePage;