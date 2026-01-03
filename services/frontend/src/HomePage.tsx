import {Button} from "@/components/ui/button.tsx";
import {Link} from "react-router";

const buttonFormat = "w-full h-12 text-xl"

function ChoreManagement() {
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
    // User objects with Id and Name
    const users = ["Joy", "Eli", "Yana", "Chase"];

    return (
        <>
            <ChoreManagement/>

            <div className="flex flex-col gap-10 items-center justify-center h-screen">
                <h1 className="text-4xl text-primary">Who Are You?</h1>

                {
                    users.map((user) => (
                        <Link to={`/${user}`} className="w-full">
                            <Button variant="secondary" className={buttonFormat}>
                                {user}
                            </Button>
                        </Link>
                    ))
                }
            </div>
        </>
    )
}

export default HomePage;