import {Button} from "@/components/ui/button.tsx";
import {Link} from "react-router";

function HomePage() {
    return (
        <div className="flex flex-col gap-4 items-center justify-center h-screen">
            <div className="w-fit flex flex-col gap-4 items-center">
                <h1 className="text-5xl text-primary">Who are you?</h1>
                <h2 className="text-2xl text-secondary-foreground">Tenants:</h2>

                <Link to="/joy" className="w-full">
                    <Button variant="secondary" className="w-full text-lg">
                        Joy
                    </Button>
                </Link>

                <Link to="/eli" className="w-full">
                    <Button variant="secondary" className="w-full text-lg">
                        Eli
                    </Button>
                </Link>

                <Link to="/yana" className="w-full">
                    <Button variant="secondary" className="w-full text-lg">
                        Yana
                    </Button>
                </Link>

                <Link to="/chase" className="w-full">
                    <Button variant="secondary" className="w-full text-lg">
                        Chase
                    </Button>
                </Link>
            </div>
        </div>

    )
}

export default HomePage;